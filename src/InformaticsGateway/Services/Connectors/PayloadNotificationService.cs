// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ardalis.GuardClauses;
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
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;

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
                    async (task) => await UploadPayloadActionBlock(task, cancellationToken).ConfigureAwait(false),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = _options.Value.Storage.Concurrentcy,
                        MaxMessagesPerTask = 1,
                        CancellationToken = cancellationToken
                    });

            _publishQueue = new ActionBlock<Payload>(
                    async (task) => await PublishPayloadActionBlock(task).ConfigureAwait(false),
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
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceStarted(ServiceName);

            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        private void BackgroundProcessing(CancellationToken cancellationToken)
        {
            _logger.ServiceRunning(ServiceName);

            while (!cancellationToken.IsCancellationRequested)
            {
                Payload payload = null;
                try
                {
                    payload = _payloadAssembler.Dequeue(cancellationToken);
                    using (_logger.BeginScope(new LoggingDataDictionary<string, object> { { "Payload", payload.Id }, { "Correlation ID", payload.CorrelationId } }))
                    {
                        _uploadQueue.Post(payload);
                        _logger.PayloadQueuedForProcessing(payload.Id, ServiceName);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _logger.ServiceCancelledWithException(ServiceName, ex);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.ServiceDisposed(ServiceName, ex);
                }
                catch (Exception ex)
                {
                    _logger.ErrorProcessingPayload(payload?.Id, ex);
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        private async Task UploadPayloadActionBlock(Payload payload, CancellationToken cancellationToken)
        {
            Guard.Against.Null(payload, nameof(payload));
            try
            {
                await Upload(payload, cancellationToken).ConfigureAwait(false);

                if (payload.Files.Count == 0)
                {
                    payload.State = Payload.PayloadState.Notify;
                    payload.ResetRetry();

                    var scope = _serviceScopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                    await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);

                    _publishQueue.Post(payload);
                    _logger.PayloadReadyToBePublished(payload.Id);
                }
            }
            catch (Exception ex)
            {
                if (payload is not null)
                {
                    payload.RetryCount++;
                    var action = await UpdatePayloadState(payload).ConfigureAwait(false);
                    if (action == PayloadAction.Updated)
                    {
                        await _uploadQueue.Post(payload, _options.Value.Storage.Retries.RetryDelays.ElementAt(payload.RetryCount - 1)).ConfigureAwait(false);
                        _logger.FailedToUpload(payload.Id, ex);
                    }
                }
            }
        }

        private async Task Upload(Payload payload, CancellationToken cancellationToken)
        {
            Guard.Against.Null(payload, nameof(payload));

            _logger.UploadingPayloadToBucket(payload.Id, _options.Value.Storage.StorageServiceBucketName);

            for (var index = payload.Files.Count - 1; index >= 0; index--)
            {
                var file = payload.Files[index];

                switch (file)
                {
                    case DicomFileStorageInfo dicom:
                        await UploadPayloadFile(payload.Id, dicom.DicomJsonUploadPath, dicom.DicomJsonFilePath, dicom.Source, dicom.Workflows, dicom.ContentType, cancellationToken).ConfigureAwait(false);
                        break;
                }
                await UploadPayloadFile(payload.Id, file.UploadPath, file.FilePath, file.Source, file.Workflows, file.ContentType, cancellationToken).ConfigureAwait(false);
                payload.UploadedFiles.Add(file.ToBlockStorageInfo(_options.Value.Storage.StorageServiceBucketName));
                payload.Files.Remove(file);
                _instanceCleanupQueue.Queue(file);
            }
        }

        private async Task UploadPayloadFile(Guid payloadId, string uploadPath, string filePath, string source, string[] workflows, string contentType, CancellationToken cancellationToken)
        {
            Guard.Against.Null(payloadId, nameof(payloadId));
            Guard.Against.NullOrWhiteSpace(uploadPath, nameof(uploadPath));
            Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));
            Guard.Against.NullOrWhiteSpace(source, nameof(source));
            Guard.Against.NullOrWhiteSpace(contentType, nameof(contentType));

            uploadPath = Path.Combine(payloadId.ToString(), uploadPath);
            _logger.UploadingFileInPayload(payloadId, filePath);
            using var stream = _fileSystem.File.OpenRead(filePath);
            var metadata = new Dictionary<string, string>
                {
                    { FileMetadataKeys.Source, source },
                    { FileMetadataKeys.Workflows, workflows.IsNullOrEmpty() ? string.Empty : string.Join(',', workflows) }
                };

            await _storageService.PutObject(_options.Value.Storage.StorageServiceBucketName, uploadPath, stream, stream.Length, contentType, metadata, cancellationToken).ConfigureAwait(false);
        }

        private async Task PublishPayloadActionBlock(Payload payload)
        {
            Guard.Against.Null(payload, nameof(payload));
            try
            {
                await NotifyPayloadReady(payload).ConfigureAwait(false);

                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                await payload.DeletePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (payload is not null)
                {
                    payload.RetryCount++;
                    var action = await UpdatePayloadState(payload).ConfigureAwait(false);
                    if (action == PayloadAction.Updated)
                    {
                        await _publishQueue.Post(payload, _options.Value.Messaging.Retries.RetryDelays.ElementAt(payload.RetryCount - 1)).ConfigureAwait(false);
                        _logger.FailedToPublishWorkflowRequest(payload.Id, ex);
                    }
                }
            }
        }

        private async Task<PayloadAction> UpdatePayloadState(Payload payload)
        {
            Guard.Against.Null(payload, nameof(payload));

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();

            try
            {
                if (payload.RetryCount > _options.Value.Storage.Retries.DelaysMilliseconds.Length)
                {
                    _logger.UploadFailureStopRetry(payload.Id);
                    await payload.DeletePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
                    return PayloadAction.Deleted;
                }
                else
                {
                    _logger.UploadFailureRetryLater(payload.Id, payload.State, payload.RetryCount);
                    await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
                    return PayloadAction.Updated;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorUpdatingPayload(payload.Id, ex);
                return PayloadAction.Updated;
            }
        }

        private async Task NotifyPayloadReady(Payload payload)
        {
            Guard.Against.Null(payload, nameof(payload));

            _logger.GenerateWorkflowRequest(payload.Id);
            var workflowRequest = new WorkflowRequestMessage
            {
                PayloadId = payload.Id,
                Workflows = payload.Workflows,
                FileCount = payload.Count,
                CorrelationId = payload.CorrelationId,
                Timestamp = payload.DateTimeCreated
            };

            workflowRequest.Payload.AddRange(payload.UploadedFiles);

            var message = new JsonMessage<WorkflowRequestMessage>(
                workflowRequest,
                payload.CorrelationId,
                string.Empty);

            _logger.PublishingWorkflowRequest(message.MessageId);

            await _messageBrokerPublisherService.Publish(
                _options.Value.Messaging.Topics.WorkflowRequest,
                message.ToMessage()).ConfigureAwait(false);

            _logger.WorkflowRequestPublished(_options.Value.Messaging.Topics.WorkflowRequest, message.MessageId);
        }

        private void RestoreFromDatabase()
        {
            _logger.StartupRestoreFromDatabase();

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
            _logger.RestoredFromDatabase(payloads.Count());
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            _uploadQueue.Complete();
            _publishQueue.Complete();
            Status = ServiceStatus.Stopped;

            _logger.ServiceStopPending(ServiceName);

            await Task.WhenAll(_uploadQueue.Completion, _publishQueue.Completion).ConfigureAwait(false);
        }
    }
}
