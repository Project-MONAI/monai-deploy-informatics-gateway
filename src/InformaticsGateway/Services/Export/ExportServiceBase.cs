// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
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
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Storage;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public abstract class ExportServiceBase : IHostedService, IMonaiService, IDisposable
    {
        private static readonly object SyncRoot = new();

        internal event EventHandler ReportActionCompleted;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly InformaticsGatewayConfiguration _configuration;
        private readonly IMessageBrokerSubscriberService _messageSubscriber;
        private readonly IMessageBrokerPublisherService _messagePublisher;
        private readonly IServiceScope _scope;
        private readonly Dictionary<string, ExportRequestEvent> _exportRequests;
        private bool _disposedValue;

        public abstract string RoutingKey { get; }
        protected abstract int Concurrency { get; }
        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public abstract string ServiceName { get; }

        /// <summary>
        /// Override the <c>ExportDataBlockCallback</c> method to customize export logic.
        /// Must update <c>State</c> to either <c>Succeeded</c> or <c>Failed</c>.
        /// </summary>
        /// <param name="outputJob"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken);

        protected ExportServiceBase(
            ILogger logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IServiceScopeFactory serviceScopeFactory,
            IStorageInfoProvider storageInfoProvider)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _scope = _serviceScopeFactory.CreateScope();

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _storageInfoProvider = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _configuration = configuration.Value;

            _messageSubscriber = _scope.ServiceProvider.GetRequiredService<IMessageBrokerSubscriberService>();
            _messagePublisher = _scope.ServiceProvider.GetRequiredService<IMessageBrokerPublisherService>();

            _exportRequests = new Dictionary<string, ExportRequestEvent>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupPolling();

            Status = ServiceStatus.Running;
            _logger.ServiceStarted(ServiceName);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            _logger.ServiceStopping(ServiceName);
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private void SetupPolling()
        {
            _messageSubscriber.Subscribe(RoutingKey, String.Empty, OnMessageReceivedCallback);
            _logger.ExportEventSubscription(ServiceName, RoutingKey);
        }

        private void OnMessageReceivedCallback(MessageReceivedEventArgs eventArgs)
        {
            if (!_storageInfoProvider.HasSpaceAvailableForExport)
            {
                _logger.ExportPausedDueToInsufficientStorageSpace(ServiceName, _storageInfoProvider.AvailableFreeSpace);
                _messageSubscriber.Reject(eventArgs.Message);
                return;
            }

            try
            {
                var executionOptions = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Concurrency,
                    MaxMessagesPerTask = 1,
                    CancellationToken = _cancellationTokenSource.Token
                };

                var exportFlow = new TransformManyBlock<ExportRequestEvent, ExportRequestDataMessage>(
                    (exportRequest) => DownloadPayloadActionCallback(exportRequest, _cancellationTokenSource.Token),
                    executionOptions);

                var exportActionBlock = new TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage>(
                    async (exportDataRequest) =>
                    {
                        if (exportDataRequest.IsFailed) return exportDataRequest;
                        return await ExportDataBlockCallback(exportDataRequest, _cancellationTokenSource.Token).ConfigureAwait(false);
                    },
                    executionOptions);

                var reportingActionBlock = new ActionBlock<ExportRequestDataMessage>(ReportingActionBlock, executionOptions);

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                exportFlow.LinkTo(exportActionBlock, linkOptions);
                exportActionBlock.LinkTo(reportingActionBlock, linkOptions);

                lock (SyncRoot)
                {
                    var exportRequest = eventArgs.Message.ConvertTo<ExportRequestEvent>();
                    if (_exportRequests.ContainsKey(exportRequest.ExportTaskId))
                    {
                        _logger.ExportRequestAlreadyQueued(exportRequest.ExportTaskId);
                        return;
                    }

                    exportRequest.MessageId = eventArgs.Message.MessageId;
                    exportRequest.DeliveryTag = eventArgs.Message.DeliveryTag;

                    _exportRequests.Add(exportRequest.ExportTaskId, exportRequest);
                    exportFlow.Post(exportRequest);
                }

                exportFlow.Complete();
                reportingActionBlock.Completion.Wait(_cancellationTokenSource.Token);
            }
            catch (AggregateException ex)
            {
                foreach (var iex in ex.InnerExceptions)
                {
                    _logger.ErrorExporting(iex);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingExportTask(ex);
            }
        }

        private IEnumerable<ExportRequestDataMessage> DownloadPayloadActionCallback(ExportRequestEvent exportRequest, CancellationToken cancellationToken)
        {
            Guard.Against.Null(exportRequest, nameof(exportRequest));
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequest.ExportTaskId }, { "CorrelationId", exportRequest.CorrelationId } });
            var scope = _serviceScopeFactory.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            foreach (var file in exportRequest.Files)
            {
                var exportRequestData = new ExportRequestDataMessage(exportRequest, file);
                try
                {
                    _logger.DownloadingFile(file);
                    Policy
                       .Handle<Exception>()
                       .WaitAndRetry(
                           _configuration.Export.Retries.RetryDelays,
                           (exception, timeSpan, retryCount, context) =>
                           {
                               _logger.ErrorDownloadingPayloadWithRetry(exception, timeSpan, retryCount);
                           })
                       .Execute(() =>
                       {
                           _logger.DownloadingFile(file);
                           var task = storageService.GetObject(_configuration.Storage.StorageServiceBucketName, file, (stream) =>
                           {
                               using var memoryStream = new MemoryStream();
                               stream.CopyTo(memoryStream);
                               exportRequestData.SetData(memoryStream.ToArray());
                           }, cancellationToken);

                           task.Wait();
                           _logger.FileReadyForExport(file);
                       });
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error downloading payload.";
                    _logger.ErrorDownloadingPayload(ex);
                    exportRequestData.SetFailed(errorMessage);
                }

                yield return exportRequestData;
            }
        }

        private void ReportingActionBlock(ExportRequestDataMessage exportRequestData)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId } });

            var exportRequest = _exportRequests[exportRequestData.ExportTaskId];
            lock (SyncRoot)
            {
                if (exportRequestData.IsFailed)
                {
                    exportRequest.FailedFiles++;
                }
                else
                {
                    exportRequest.SucceededFiles++;
                }

                if (exportRequestData.Messages.Any())
                {
                    exportRequest.AddErrorMessages(exportRequestData.Messages);
                }

                if (!exportRequest.IsCompleted)
                {
                    return;
                }
            }

            _logger.ExportCompleted(exportRequest.FailedFiles, exportRequest.Files.Count());

            var exportCompleteEvent = new ExportCompleteEvent(exportRequest);
            var jsonMessage = new JsonMessage<ExportCompleteEvent>(exportCompleteEvent, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequest.CorrelationId, exportRequest.DeliveryTag);

            Policy
               .Handle<Exception>()
               .WaitAndRetry(
                   _configuration.Export.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorAcknowledgingMessageWithRetry(exception, timeSpan, retryCount);
                   })
               .Execute(() =>
               {
                   _logger.SendingAckowledgement();
                   _messageSubscriber.Acknowledge(jsonMessage);
               });

            Policy
               .Handle<Exception>()
               .WaitAndRetry(
                   _configuration.Export.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorPublishingExportCompleteEventWithRetry(exception, timeSpan, retryCount);
                   })
               .Execute(() =>
               {
                   _logger.PublishingExportCompleteEvent();
                   _messagePublisher.Publish(_configuration.Messaging.Topics.ExportComplete, jsonMessage.ToMessage());
               });

            lock (SyncRoot)
            {
                _exportRequests.Remove(exportRequestData.ExportTaskId);
            }

            if (ReportActionCompleted != null)
            {
                _logger.CallingReportActionCompletedCallback();
                ReportActionCompleted(this, EventArgs.Empty);
            }
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
