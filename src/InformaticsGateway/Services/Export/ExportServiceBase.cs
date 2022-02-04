﻿// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public abstract class ExportServiceBase : IHostedService, IMonaiService
    {
        private static readonly object SyncRoot = new();

        internal event EventHandler ReportActionStarted;

        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly InformaticsGatewayConfiguration _configuration;
        private readonly IMessageBrokerSubscriberService _messageSubscriber;
        private readonly IMessageBrokerPublisherService _messagePublisher;
        private readonly IServiceScope _scope;
        private readonly Dictionary<string, ExportRequestMessage> _exportRequets;
        private readonly string _exportQueueName;
        private TransformManyBlock<ExportRequestMessage, ExportRequestDataMessage> _exportFlow;

        public abstract string RoutingKey { get; }
        protected abstract int Concurrentcy { get; }
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

            _exportRequets = new Dictionary<string, ExportRequestMessage>();
            _exportQueueName = _configuration.Messaging.Topics.ExportRequestQueue;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupExportFlow(cancellationToken);
            SetupPolling();

            Status = ServiceStatus.Running;
            _logger.LogInformation($"{ServiceName} started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{ServiceName} is stopping.");
            _exportFlow.Complete();
            _exportFlow.Completion.Wait(cancellationToken);
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private void SetupExportFlow(CancellationToken cancellationToken)
        {
            try
            {
                var executionOptions = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Concurrentcy,
                    MaxMessagesPerTask = 1,
                    CancellationToken = cancellationToken
                };

                _exportFlow = new TransformManyBlock<ExportRequestMessage, ExportRequestDataMessage>(
                    (exportRequest) => DownloadPayloadActionCallback(exportRequest, cancellationToken),
                    executionOptions);

                var exportActionBlock = new TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage>(
                    async (exportDataRequest) =>
                    {
                        if (exportDataRequest.IsFailed) return exportDataRequest;
                        return await ExportDataBlockCallback(exportDataRequest, cancellationToken);
                    },
                    executionOptions);

                var reportingActionBlock = new ActionBlock<ExportRequestDataMessage>(ReportingActionBlock, executionOptions);

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                _exportFlow.LinkTo(exportActionBlock, linkOptions);
                exportActionBlock.LinkTo(reportingActionBlock, linkOptions);

                _logger.Log(LogLevel.Information, $"{ServiceName} completed workflow setup.");
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

        private void SetupPolling()
        {
            _messageSubscriber.Subscribe(RoutingKey, _exportQueueName, OnMessageReceivedCallback);
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
            var exportRequest = eventArgs.Message.ConvertTo<ExportRequestMessage>();
            exportRequest.MessageId = eventArgs.Message.MessageId;

            _exportRequets.Add(exportRequest.ExportTaskId, exportRequest);
            _exportFlow.Post(exportRequest);
        }

        private IEnumerable<ExportRequestDataMessage> DownloadPayloadActionCallback(ExportRequestMessage exportRequest, CancellationToken cancellationToken)
        {
            Guard.Against.Null(exportRequest, nameof(exportRequest));
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequest.ExportTaskId }, { "CorrelationId", exportRequest.CorrelationId } });
            var scope = _serviceScopeFactory.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            var exportRequestData = new ExportRequestDataMessage(exportRequest);
            foreach (var file in exportRequest.Files)
            {
                try
                {
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
                           var task = storageService.GetObject(_configuration.Storage.StorageServiceBucketName, file, (stream) =>
                           {
                               stream.Seek(0, System.IO.SeekOrigin.Begin);
                               using var memoryStream = new MemoryStream();
                               stream.CopyTo(memoryStream);
                               exportRequestData.SetData(memoryStream.ToArray());
                           }, cancellationToken);

                           task.Wait();
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

            var exportRequest = _exportRequets[exportRequestData.ExportTaskId];
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

            if (ReportActionStarted != null)
            {
                _logger.Log(LogLevel.Debug, $"Calling ReportActionStarted callback.");
                ReportActionStarted(this, null);
            }

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
        }
    }
}