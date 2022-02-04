// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    /// <summary>
    /// The Payload Notification Service class includes 2 action blocks (queues) that process
    /// any payloads that are in eitehr <c>Payload.PayloadState.Upload</c> or
    /// <c>Payload.PayloadState.Notify</c> states. It also queries the payload assembler for
    /// payloads that are ready for upload in the background.
    /// </summary>
    internal class PayloadNotificationService : IHostedService, IMonaiService
    {
        /// <summary>
        /// Internal use to indicate if payload was updated or deleted.
        /// </summary>
        private enum PayloadAction
        {
            Updated,
            Deleted
        }

        private static readonly Payload.PayloadState[] SupportedStates = new[] { Payload.PayloadState.Upload, Payload.PayloadState.Notify };
        private readonly IFileSystem _fileSystem;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IStorageService _storageService;
        private readonly ILogger<PayloadNotificationService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageBrokerPublisherService _messageBrokerPublisherService;
        private readonly IInstanceCleanupQueue _instanceCleanupQueue;
        private ActionBlock<Payload> _uploadQueue;
        private ActionBlock<Payload> _publishQueue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public string ServiceName => "Payload Notification Service";

        public PayloadNotificationService(IFileSystem fileSystem,
                                          IPayloadAssembler payloadAssembler,
                                          IStorageService storageService,
                                          ILogger<PayloadNotificationService> logger,
                                          IOptions<InformaticsGatewayConfiguration> options,
                                          IServiceScopeFactory serviceScopeFactory,
                                          IMessageBrokerPublisherService messageBrokerPublisherService,
                                          IInstanceCleanupQueue instanceCleanupQueue)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _messageBrokerPublisherService = messageBrokerPublisherService ?? throw new ArgumentNullException(nameof(messageBrokerPublisherService));
            _instanceCleanupQueue = instanceCleanupQueue ?? throw new ArgumentNullException(nameof(instanceCleanupQueue));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _uploadQueue = new ActionBlock<Payload>(
                    async (task) => await UploadPayloadActionBlock(task, cancellationToken),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = _options.Value.Storage.Concurrentcy,
                        MaxMessagesPerTask = 1,
                        CancellationToken = cancellationToken
                    });

            _publishQueue = new ActionBlock<Payload>(
                    async (task) => await PublishPayloadActionBlock(task),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = 1,
                        MaxMessagesPerTask = 1,
                        CancellationToken = cancellationToken
                    });

            RestoreFromDatabase();

            var task = Task.Run(() =>
            {
                BackgroundProcessing(cancellationToken);
            });

            Status = ServiceStatus.Running;
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        private void BackgroundProcessing(CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, $"{ServiceName} is running.");

            while (!cancellationToken.IsCancellationRequested)
            {
                Payload payload = null;
                try
                {
                    payload = _payloadAssembler.Dequeue(cancellationToken);
                    using (_logger.BeginScope(new LoggingDataDictionary<string, object> { { "Payload", payload.Id }, { "Correlation ID", payload.CorrelationId } }))
                    {
                        _uploadQueue.Post(payload);
                        _logger.Log(LogLevel.Information, $"Payload {payload.Id} added to {ServiceName} for processing.");
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.Log(LogLevel.Warning, ex, $"{ServiceName} canceled.");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.Log(LogLevel.Warning, ex, $"{ServiceName} may be disposed.");
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex, $"Error processing request: Payload = {payload?.Id}");
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.Log(LogLevel.Information, "Cancellation requested.");
        }

        private async Task UploadPayloadActionBlock(Payload payload, CancellationToken cancellationToken)
        {
            try
            {
                await Upload(payload, cancellationToken);

                if (payload.Files.Count == 0)
                {
                    payload.State = Payload.PayloadState.Notify;
                    payload.ResetRetry();

                    var scope = _serviceScopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                    await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository);

                    _publishQueue.Post(payload);
                    _logger.Log(LogLevel.Information, $"Payload {payload.Id} ready to be published.");
                }
            }
            catch (Exception ex)
            {
                if (payload is not null)
                {
                    payload.RetryCount++;
                    var action = await UpdatePayloadState(payload);
                    if (action == PayloadAction.Updated)
                    {
                        await _uploadQueue.Post(payload, _options.Value.Storage.Retries.RetryDelays.ElementAt(payload.RetryCount - 1));
                        _logger.Log(LogLevel.Warning, ex, $"Failed to upload payload {payload.Id}; added back to queue for retry.");
                    }
                }
            }
        }

        private async Task Upload(Payload payload, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, $"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.");

            for (var index = payload.Files.Count - 1; index >= 0; index--)
            {
                var file = payload.Files[index];

                switch (file)
                {
                    case DicomFileStorageInfo dicom:
                        await UploadPayloadFile(payload.Id, dicom.DicomJsonUploadPath, dicom.DicomJsonFilePath, dicom.Source, dicom.Workflows, dicom.ContentType, cancellationToken);
                        break;
                }
                await UploadPayloadFile(payload.Id, file.UploadPath, file.FilePath, file.Source, file.Workflows, file.ContentType, cancellationToken);
                payload.UploadedFiles.Add(file.ToBlockStorageInfo(_options.Value.Storage.StorageServiceBucketName));
                payload.Files.Remove(file);
                _instanceCleanupQueue.Queue(file);
            }
        }

        private async Task UploadPayloadFile(Guid payloadId, string uploadPath, string filePath, string source, string[] workflows, string contentType, CancellationToken cancellationToken)
        {
            uploadPath = Path.Combine(payloadId.ToString(), uploadPath);
            _logger.Log(LogLevel.Debug, $"Uploading file {filePath} from payload {payloadId} to storage service.");
            using var stream = _fileSystem.File.OpenRead(filePath);
            var metadata = new Dictionary<string, string>
                {
                    { FileMetadataKeys.Source, source },
                    { FileMetadataKeys.Workflows, workflows.IsNullOrEmpty() ? string.Empty : string.Join(',', workflows) }
                };

            await _storageService.PutObject(_options.Value.Storage.StorageServiceBucketName, uploadPath, stream, stream.Length, contentType, metadata, cancellationToken);
        }

        private async Task PublishPayloadActionBlock(Payload payload)
        {
            try
            {
                await NotifyPayloadReady(payload);

                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                await payload.DeletePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository);
            }
            catch (Exception ex)
            {
                if (payload is not null)
                {
                    payload.RetryCount++;
                    var action = await UpdatePayloadState(payload);
                    if (action == PayloadAction.Updated)
                    {
                        await _publishQueue.Post(payload, _options.Value.Messaging.Retries.RetryDelays.ElementAt(payload.RetryCount - 1));
                        _logger.Log(LogLevel.Warning, ex, $"Failed to publish workflow request for payload {payload.Id}; added back to queue for retry.");
                    }
                }
            }
        }

        private async Task<PayloadAction> UpdatePayloadState(Payload payload)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();

            try
            {
                if (payload.RetryCount > _options.Value.Storage.Retries.DelaysMilliseconds.Length)
                {
                    _logger.Log(LogLevel.Error, $"Reached maximum number of retries for payload {payload.Id}, giving up.");
                    await payload.DeletePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository);
                    return PayloadAction.Deleted;
                }
                else
                {
                    _logger.Log(LogLevel.Error, $"Updating payload state={payload.State}, retries={payload.RetryCount}.");
                    await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository);
                    return PayloadAction.Updated;
                }
            }
            catch (Exception iex)
            {
                _logger.Log(LogLevel.Error, iex, $"Error updating payload failure: Payload = {payload?.Id}");
                return PayloadAction.Updated;
            }
        }

        private Task NotifyPayloadReady(Payload payload)
        {
            _logger.Log(LogLevel.Debug, $"Generating workflow request message for payload {payload.Id}...");
            var message = new JsonMessage<WorkflowRequestMessage>(
                new WorkflowRequestMessage
                {
                    PayloadId = payload.Id,
                    Workflows = payload.Workflows,
                    FileCount = payload.Count,
                    CorrelationId = payload.CorrelationId,
                    Timestamp = payload.DateTimeCreated
                },
                payload.CorrelationId,
                string.Empty);

            _logger.Log(LogLevel.Information, $"Publishing workflow request message ID={message.MessageId}...");

            var task = _messageBrokerPublisherService.Publish(
                _options.Value.Messaging.Topics.WorkflowRequest,
                message.ToMessage());
            _logger.Log(LogLevel.Information, $"Workflow request published to {_options.Value.Messaging.Topics.WorkflowRequest}, message ID={message.MessageId}.");
            return task;
        }

        private void RestoreFromDatabase()
        {
            _logger.Log(LogLevel.Information, "Restoring payloads from database.");

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();

            var payloads = repository.AsQueryable().Where(p => SupportedStates.Contains(p.State));
            foreach (var payload in payloads)
            {
                if (payload.State == Payload.PayloadState.Upload)
                {
                    _uploadQueue.Post(payload);
                }
                else if (payload.State == Payload.PayloadState.Notify)
                {
                    _publishQueue.Post(payload);
                }
            }
            _logger.Log(LogLevel.Information, $"{payloads.Count()} payloads restored from database.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, $"Stopping {ServiceName}.");
            _uploadQueue.Complete();
            _publishQueue.Complete();
            Status = ServiceStatus.Stopped;
            _logger.Log(LogLevel.Information, $"{ServiceName} stopped, waiting for queues to complete...");
            return Task.WhenAll(_uploadQueue.Completion, _publishQueue.Completion);
        }
    }
}
