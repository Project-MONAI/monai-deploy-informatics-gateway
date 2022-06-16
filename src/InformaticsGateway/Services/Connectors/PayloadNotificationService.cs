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
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Storage.API;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    /// <summary>
    /// The Payload Notification Service class includes 2 action blocks (queues) that process
    /// any payloads that are in eitehr <c>Payload.PayloadState.Upload</c> or
    /// <c>Payload.PayloadState.Notify</c> states. It also queries the payload assembler for
    /// payloads that are ready for upload in the background.
    /// </summary>
    internal class PayloadNotificationService : IHostedService, IMonaiService, IDisposable
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

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PayloadNotificationService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IServiceScope _scope;
        private readonly IFileSystem _fileSystem;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IStorageService _storageService;
        private readonly IInstanceCleanupQueue _instanceCleanupQueue;
        private readonly IMessageBrokerPublisherService _messageBrokerPublisherService;

        private ActionBlock<Payload> _uploadQueue;
        private ActionBlock<Payload> _publishQueue;
        private bool _disposedValue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public string ServiceName => "Payload Notification Service";

        public PayloadNotificationService(IServiceScopeFactory serviceScopeFactory,
                                          ILogger<PayloadNotificationService> logger,
                                          IOptions<InformaticsGatewayConfiguration> options)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _scope = _serviceScopeFactory.CreateScope();

            _fileSystem = _scope.ServiceProvider.GetRequiredService<IFileSystem>() ?? throw new ServiceNotFoundException(nameof(IFileSystem));
            _payloadAssembler = _scope.ServiceProvider.GetRequiredService<IPayloadAssembler>() ?? throw new ServiceNotFoundException(nameof(IPayloadAssembler));
            _storageService = _scope.ServiceProvider.GetRequiredService<IStorageService>() ?? throw new ServiceNotFoundException(nameof(IStorageService));
            _instanceCleanupQueue = _scope.ServiceProvider.GetRequiredService<IInstanceCleanupQueue>() ?? throw new ServiceNotFoundException(nameof(IInstanceCleanupQueue));
            _messageBrokerPublisherService = _scope.ServiceProvider.GetRequiredService<IMessageBrokerPublisherService>() ?? throw new ServiceNotFoundException(nameof(IMessageBrokerPublisherService));
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

                if (payload.IsUploadComplete())
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
                        if (!string.IsNullOrWhiteSpace(dicom.JsonFilePath))
                        {
                            await UploadPayloadFile(payload.Id, dicom.JsonUploadFilePath, dicom.JsonFilePath, dicom.Source, dicom.Workflows, dicom.ContentType, cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }
                await UploadPayloadFile(payload.Id, file.UploadFilePath, file.FilePath, file.Source, file.Workflows, file.ContentType, cancellationToken).ConfigureAwait(false);
                file.SetUploaded();
                _instanceCleanupQueue.Queue(file);
            }
        }

        private async Task UploadPayloadFile(Guid payloadId, string destinationPath, string sourcePath, string source, List<string> workflows, string contentType, CancellationToken cancellationToken)
        {
            Guard.Against.Null(payloadId, nameof(payloadId));
            Guard.Against.NullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.Against.NullOrWhiteSpace(sourcePath, nameof(sourcePath));
            Guard.Against.NullOrWhiteSpace(source, nameof(source));
            Guard.Against.NullOrWhiteSpace(contentType, nameof(contentType));

            destinationPath = Path.Combine(payloadId.ToString(), destinationPath);
            _logger.UploadingFileInPayload(payloadId, sourcePath);
            using var stream = _fileSystem.File.OpenRead(sourcePath);
            var metadata = new Dictionary<string, string>
                {
                    { FileMetadataKeys.Source, source },
                    { FileMetadataKeys.Workflows, workflows.IsNullOrEmpty() ? string.Empty : string.Join(',', workflows) }
                };

            await _storageService.PutObjectAsync(_options.Value.Storage.StorageServiceBucketName, destinationPath, stream, stream.Length, contentType, metadata, cancellationToken).ConfigureAwait(false);
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

            var workflowRequest = new WorkflowRequestEvent
            {
                Bucket = _options.Value.Storage.StorageServiceBucketName,
                PayloadId = payload.Id,
                Workflows = payload.GetWorkflows(),
                FileCount = payload.Count,
                CorrelationId = payload.CorrelationId,
                Timestamp = payload.DateTimeCreated,
                CalledAeTitle = payload.CalledAeTitle,
                CallingAeTitle = payload.CallingAeTitle,
            };

            workflowRequest.AddFiles(payload.GetUploadedFiles(_options.Value.Storage.StorageServiceBucketName).AsEnumerable());

            var message = new JsonMessage<WorkflowRequestEvent>(
                workflowRequest,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _scope.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
