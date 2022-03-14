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
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public abstract class ExportServiceBase : IHostedService, IMonaiService
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
        private readonly Dictionary<string, ExportRequestMessage> _exportRequests;

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

        public ExportServiceBase(
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

            _exportRequests = new Dictionary<string, ExportRequestMessage>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupPolling();

            Status = ServiceStatus.Running;
            _logger.LogInformation($"{ServiceName} started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            _logger.LogInformation($"{ServiceName} is stopping.");
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private void SetupPolling()
        {
            _messageSubscriber.Subscribe(RoutingKey, String.Empty, OnMessageReceivedCallback);
            _logger.Log(LogLevel.Information, $"{ServiceName} subscribed to {RoutingKey} messages.");
        }

        private void OnMessageReceivedCallback(MessageReceivedEventArgs eventArgs)
        {
            if (!_storageInfoProvider.HasSpaceAvailableForExport)
            {
                _logger.Log(LogLevel.Warning, $"Export service paused due to insufficient storage space.  Available storage space: {_storageInfoProvider.AvailableFreeSpace:D}.");
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

                var exportFlow = new TransformManyBlock<ExportRequestMessage, ExportRequestDataMessage>(
                    (exportRequest) => DownloadPayloadActionCallback(exportRequest, _cancellationTokenSource.Token),
                    executionOptions);

                var exportActionBlock = new TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage>(
                    async (exportDataRequest) =>
                    {
                        if (exportDataRequest.IsFailed) return exportDataRequest;
                        return await ExportDataBlockCallback(exportDataRequest, _cancellationTokenSource.Token);
                    },
                    executionOptions);

                var reportingActionBlock = new ActionBlock<ExportRequestDataMessage>(ReportingActionBlock, executionOptions);

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                exportFlow.LinkTo(exportActionBlock, linkOptions);
                exportActionBlock.LinkTo(reportingActionBlock, linkOptions);

                lock (SyncRoot)
                {
                    var exportRequest = eventArgs.Message.ConvertTo<ExportRequestMessage>();
                    if (_exportRequests.ContainsKey(exportRequest.ExportTaskId))
                    {
                        _logger.Log(LogLevel.Warning, $"The export request {exportRequest.ExportTaskId} is already queued for export.");
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
                    _logger.Log(LogLevel.Error, iex, "Error occurred while exporting.");
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error processing export task.");
            }
        }

        private IEnumerable<ExportRequestDataMessage> DownloadPayloadActionCallback(ExportRequestMessage exportRequest, CancellationToken cancellationToken)
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
                    _logger.Log(LogLevel.Debug, $"Downloading file {file}...");
                    Policy
                       .Handle<Exception>()
                       .WaitAndRetry(
                           _configuration.Export.Retries.RetryDelays,
                           (exception, timeSpan, retryCount, context) =>
                           {
                               _logger.Log(LogLevel.Error, exception, $"Error downloading payload. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                           })
                       .Execute(() =>
                       {
                           _logger.Log(LogLevel.Debug, $"Downloading {file}...");
                           var task = storageService.GetObject(_configuration.Storage.StorageServiceBucketName, file, (stream) =>
                           {
                               using var memoryStream = new MemoryStream();
                               stream.CopyTo(memoryStream);
                               exportRequestData.SetData(memoryStream.ToArray());
                           }, cancellationToken);

                           task.Wait();
                           _logger.Log(LogLevel.Debug, $"File {file} ready for export.");
                       });
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error downloading payload: {ex.Message}.";
                    _logger.Log(LogLevel.Error, ex, errorMessage);
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

            _logger.Log(LogLevel.Information, $"Export task completed with {exportRequest.FailedFiles} failures out of {exportRequest.Files.Count()}");

            var exportCompleteMessage = new ExportCompleteMessage(exportRequest);
            var jsonMessage = new JsonMessage<ExportCompleteMessage>(exportCompleteMessage, exportRequest.CorrelationId, exportRequest.DeliveryTag);

            Policy
               .Handle<Exception>()
               .WaitAndRetry(
                   _configuration.Export.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.Log(LogLevel.Error, exception, $"Error acknowledging message. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                   })
               .Execute(() =>
               {
                   _logger.Log(LogLevel.Information, $"Sending acknowledgement.");
                   _messageSubscriber.Acknowledge(jsonMessage);
               });

            Policy
               .Handle<Exception>()
               .WaitAndRetry(
                   _configuration.Export.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.Log(LogLevel.Error, exception, $"Error publishing message. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                   })
               .Execute(() =>
               {
                   _logger.Log(LogLevel.Information, $"Publishing export complete message.");
                   _messagePublisher.Publish(_configuration.Messaging.Topics.ExportComplete, jsonMessage.ToMessage());
               });

            lock (SyncRoot)
            {
                _exportRequests.Remove(exportRequestData.ExportTaskId);
            }

            if (ReportActionCompleted != null)
            {
                _logger.Log(LogLevel.Debug, $"Calling ReportActionCompleted callback.");
                ReportActionCompleted(this, null);
            }
        }
    }
}
