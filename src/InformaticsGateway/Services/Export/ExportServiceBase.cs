/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using FellowOakDicom;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Storage.API;
using Polly;
using System.Net.Sockets;
using Monai.Deploy.InformaticsGateway.Api.Models;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public abstract class ExportServiceBase : IHostedService, IMonaiService, IDisposable
    {
        protected static readonly object SyncRoot = new();

        internal event EventHandler? ReportActionCompleted;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        protected readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly InformaticsGatewayConfiguration _configuration;
        protected readonly IMessageBrokerSubscriberService MessageSubscriber;
        protected readonly IMessageBrokerPublisherService MessagePublisher;
        private readonly IServiceScope _scope;
        protected readonly Dictionary<string, ExportRequestEventDetails> ExportRequests;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private bool _disposedValue;
        private ulong _activeWorkers = 0;
        private readonly IDicomToolkit _dicomToolkit;

        public abstract string RoutingKey { get; }
        protected abstract ushort Concurrency { get; }
        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public abstract string ServiceName { get; }

        protected string ExportCompleteTopic { get; set; }

        /// <summary>
        /// Override the <c>ExportDataBlockCallback</c> method to customize export logic.
        /// Must update <c>State</c> to either <c>Succeeded</c> or <c>Failed</c>.
        /// </summary>
        /// <param name="outputJob"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken);

        protected abstract Task ProcessMessage(MessageReceivedEventArgs eventArgs);

        protected ExportServiceBase(
            ILogger logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IServiceScopeFactory serviceScopeFactory,
            IDicomToolkit dicomToolkit)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ServiceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _scope = ServiceScopeFactory.CreateScope();
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));


            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration.Value;

            ExportCompleteTopic = _configuration.Messaging.Topics.ExportComplete;
            MessageSubscriber = _scope.ServiceProvider.GetRequiredService<IMessageBrokerSubscriberService>();
            MessagePublisher = _scope.ServiceProvider.GetRequiredService<IMessageBrokerPublisherService>();
            _storageInfoProvider = _scope.ServiceProvider.GetRequiredService<IStorageInfoProvider>();

            ExportRequests = new Dictionary<string, ExportRequestEventDetails>();

            MessageSubscriber.OnConnectionError += (sender, args) =>
            {
                _logger.MessagingServiceErrorRecover(args.ErrorMessage);
                SetupPolling();
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetupPolling();

            Status = ServiceStatus.Running;
            _logger.ServiceStarted(ServiceName);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            _logger.ServiceStopping(ServiceName);
            Status = ServiceStatus.Stopped;
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
            await Task.Delay(250).ConfigureAwait(false);
#pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods
            _cancellationTokenSource.Dispose();
            return;
        }

        private void SetupPolling()
        {
            MessageSubscriber.SubscribeAsync(RoutingKey, RoutingKey, OnMessageReceivedCallback, prefetchCount: Concurrency);
            _logger.ExportEventSubscription(ServiceName, RoutingKey);
        }

        protected virtual async Task OnMessageReceivedCallback(MessageReceivedEventArgs eventArgs)
        {
            using var loggerScope = _logger.BeginScope(new Messaging.Common.LoggingDataDictionary<string, object> {
                { "ThreadId", Environment.CurrentManagedThreadId },
            });

            if (!_storageInfoProvider.HasSpaceAvailableForExport)
            {
                _logger.ExportServiceStoppedDueToLowStorageSpace(_storageInfoProvider.AvailableFreeSpace);
                MessageSubscriber.Reject(eventArgs.Message);
                return;
            }

            if (Interlocked.Read(ref _activeWorkers) >= Concurrency)
            {
                _logger.ExceededMaxmimumNumberOfWorkers(ServiceName, _activeWorkers);
                await Task.Delay(200).ConfigureAwait(false); // small delay to stop instantly dead lettering the next message.
                MessageSubscriber.Reject(eventArgs.Message);
                return;
            }

            Interlocked.Increment(ref _activeWorkers);
            try
            {
                await ProcessMessage(eventArgs).ConfigureAwait(false);
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
            finally
            {
                Interlocked.Decrement(ref _activeWorkers);
            }
        }

        TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage> GetoutputDataEngineBlock(ExecutionDataflowBlockOptions executionOptions)
        {
            return new TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage>(
                async (exportDataRequest) =>
                {
                    try
                    {
                        if (exportDataRequest.IsFailed) return exportDataRequest;
                        return await ExecuteOutputDataEngineCallback(exportDataRequest).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        exportDataRequest.SetFailed(FileExportStatus.ServiceError, $"failed to execute plugin {e.Message}");
                        return exportDataRequest;
                    }
                },
                executionOptions);
        }

        TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage> GetxportActionBlock(ExecutionDataflowBlockOptions executionOptions)
        {
            return new TransformBlock<ExportRequestDataMessage, ExportRequestDataMessage>(
                async (exportDataRequest) =>
                {
                    try
                    {
                        if (exportDataRequest.IsFailed) return exportDataRequest;
                        return await ExportDataBlockCallback(exportDataRequest, _cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {

                        exportDataRequest.SetFailed(FileExportStatus.ServiceError, $"Failed during export {e.Message}");
                        return exportDataRequest;
                    }

                },
                executionOptions);
        }

        protected (TransformManyBlock<ExportRequestEventDetails, ExportRequestDataMessage>, ActionBlock<ExportRequestDataMessage>) SetupActionBlocks()
        {
            var executionOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Concurrency,
                MaxMessagesPerTask = 1,
                CancellationToken = _cancellationTokenSource.Token
            };

            var exportFlow = new TransformManyBlock<ExportRequestEventDetails, ExportRequestDataMessage>(
                exportRequest => DownloadPayloadActionCallback(exportRequest, _cancellationTokenSource.Token),
                executionOptions);

            var outputDataEngineBLock = GetoutputDataEngineBlock(executionOptions);

            var exportActionBlock = GetxportActionBlock(executionOptions);

            var reportingActionBlock = new ActionBlock<ExportRequestDataMessage>(ReportingActionBlock, executionOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            exportFlow.LinkTo(outputDataEngineBLock, linkOptions);
            outputDataEngineBLock.LinkTo(exportActionBlock, linkOptions);
            exportActionBlock.LinkTo(reportingActionBlock, linkOptions);

            return (exportFlow, reportingActionBlock);
        }

        protected void HandleCStoreException(Exception ex, ExportRequestDataMessage exportRequestData)
        {
            var exception = ex;
            var fillStatus = FileExportStatus.ServiceError;

            if (exception is AggregateException)
            {
                exception = exception.InnerException!;
            }

            var errorMessage = $"Job failed with error: {exception.Message}.";

            switch (exception)
            {
                case DicomAssociationAbortedException abortEx:
                    errorMessage = $"Association aborted with reason {abortEx.AbortReason}.";
                    break;
                case DicomAssociationRejectedException rejectEx:
                    errorMessage = $"Association rejected with reason {rejectEx.RejectReason}.";
                    break;
                case SocketException socketException:
                    errorMessage = $"Association aborted with error {socketException.Message}.";
                    break;
                case ConfigurationException configException:
                    errorMessage = $"{configException.Message}";
                    fillStatus = FileExportStatus.ConfigurationError;
                    break;
                case ExternalAppExeception appException:
                    errorMessage = $"{appException.Message}";
                    break;
            }

            _logger.ExportException(errorMessage, ex);
            exportRequestData.SetFailed(fillStatus, errorMessage);
        }

        // TPL doesn't yet support IAsyncEnumerable
        // https://github.com/dotnet/runtime/issues/30863
        private IEnumerable<ExportRequestDataMessage> DownloadPayloadActionCallback(ExportRequestEventDetails exportRequest, CancellationToken cancellationToken)
        {
            Guard.Against.Null(exportRequest, nameof(exportRequest));
            using var loggerScope = _logger.BeginScope(new Api.LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequest.ExportTaskId }, { "CorrelationId", exportRequest.CorrelationId } });
            var scope = ServiceScopeFactory.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            foreach (var file in exportRequest.Files)
            {
                var exportRequestData = new ExportRequestDataMessage(exportRequest, file);
                try
                {
                    _logger.DownloadingFile(file);
                    var task = Policy
                       .Handle<Exception>()
                       .WaitAndRetryAsync(
                           _configuration.Export.Retries.RetryDelays,
                           (exception, timeSpan, retryCount, context) =>
                           {
                               _logger.ErrorDownloadingPayloadWithRetry(exception, timeSpan, retryCount);
                           })
                       .ExecuteAsync(async () =>
                       {
                           _logger.DownloadingFile(file);
                           var stream = (await storageService.GetObjectAsync(_configuration.Storage.StorageServiceBucketName, file, cancellationToken).ConfigureAwait(false) as MemoryStream)!;
                           exportRequestData.SetData(stream.ToArray());
                           _logger.FileReadyForExport(file);
                           ExportCompleteCallback(exportRequestData).GetAwaiter().GetResult();
                       });

                    task.Wait(cancellationToken);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error downloading payload.";
                    _logger.ErrorDownloadingPayload(ex);
                    exportRequestData.SetFailed(FileExportStatus.DownloadError, errorMessage);
                }

                yield return exportRequestData;
            }
        }

        protected virtual async Task<ExportRequestDataMessage> ExecuteOutputDataEngineCallback(ExportRequestDataMessage exportDataRequest)
        {
            using var loggerScope = _logger.BeginScope(new Messaging.Common.LoggingDataDictionary<string, object> {
                { "WorkflowInstanceId", exportDataRequest.WorkflowInstanceId },
                { "TaskId", exportDataRequest.ExportTaskId }
            });
            var outputDataEngine = _scope.ServiceProvider.GetService<IOutputDataPlugInEngine>() ?? throw new ServiceNotFoundException(nameof(IOutputDataPlugInEngine));

            outputDataEngine.Configure(exportDataRequest.PlugInAssemblies);
            return await outputDataEngine.ExecutePlugInsAsync(exportDataRequest).ConfigureAwait(false);
        }

        private static void HandleStatus(ExportRequestDataMessage exportRequestData, ExportRequestEventDetails exportRequest)
        {
            lock (SyncRoot)
            {
                exportRequest.FileStatuses.Add(exportRequestData.Filename, exportRequestData.ExportStatus);
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
            }
        }

        private void ReportingActionBlock(ExportRequestDataMessage exportRequestData)
        {
            var exportRequest = ExportRequests[exportRequestData.ExportTaskId];
            HandleStatus(exportRequestData, exportRequest);
            if (!exportRequest.IsCompleted)
            {
                return;
            }

            using var loggerScope = _logger.BeginScope(new Api.LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId } });
            _logger.ExportCompleted(exportRequest.FailedFiles, exportRequest.Files.Count(), exportRequest.Duration.TotalMilliseconds);

            var exportCompleteEvent = new ExportCompleteEvent(exportRequest, exportRequest.Status, exportRequest.FileStatuses);

            var jsonMessage = new JsonMessage<ExportCompleteEvent>(exportCompleteEvent, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequest.CorrelationId, exportRequest.DeliveryTag);

            FinaliseMessage(jsonMessage);

            lock (SyncRoot)
            {
                ExportRequests.Remove(exportRequestData.ExportTaskId);
            }

            if (ReportActionCompleted != null)
            {
                _logger.CallingReportActionCompletedCallback();
                ReportActionCompleted(this, EventArgs.Empty);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task ExportCompleteCallback(ExportRequestDataMessage exportRequestData) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private void FinaliseMessage(JsonMessage<ExportCompleteEvent> jsonMessage)
        {
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
                   MessagePublisher.Publish(ExportCompleteTopic, jsonMessage.ToMessage());
               });

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
                   _logger.SendingAcknowledgement();
                   MessageSubscriber.Acknowledge(jsonMessage);
               });
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

        private async Task<DestinationApplicationEntity> LookupDestinationAsync(string destinationName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destinationName))
            {
                throw new ConfigurationException("Export task does not have destination set.");
            }

            using var scope = ServiceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDestinationApplicationEntityRepository>();
            var destination = await repository.FindByNameAsync(destinationName, cancellationToken).ConfigureAwait(false);

            return destination is null
                ? throw new ConfigurationException($"Specified destination '{destinationName}' does not exist.")
                : destination;
        }

        protected virtual async Task<DestinationApplicationEntity?> GetDestination(ExportRequestDataMessage exportRequestData, string destinationName, CancellationToken cancellationToken)
        {
            try
            {
                return await LookupDestinationAsync(destinationName, cancellationToken).ConfigureAwait(false);
            }
            catch (ConfigurationException ex)
            {
                HandleCStoreException(ex, exportRequestData);
                return null;
            }
        }

        protected virtual async Task HandleDesination(ExportRequestDataMessage exportRequestData, string destinationName, CancellationToken cancellationToken)
        {
            Guard.Against.Null(exportRequestData, nameof(exportRequestData));

            var manualResetEvent = new ManualResetEvent(false);
            var destination = await GetDestination(exportRequestData, destinationName, cancellationToken).ConfigureAwait(false);
            if (destination is null)
            {
                return;
            }

            try
            {
                await ExecuteExport(exportRequestData, manualResetEvent, destination!, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleCStoreException(ex, exportRequestData);
            }
        }

        private async Task<bool> GenerateRequestsAsync(
            ExportRequestDataMessage exportRequestData,
            IDicomClient client,
            ManualResetEvent manualResetEvent)
        {
            DicomFile dicomFile;
            try
            {
                dicomFile = _dicomToolkit.Load(exportRequestData.FileContent);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error reading DICOM file: {ex.Message}";
                _logger.ExportException(errorMessage, ex);
                exportRequestData.SetFailed(FileExportStatus.UnsupportedDataType, errorMessage);
                return false;
            }

            try
            {
                var request = new DicomCStoreRequest(dicomFile);

                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        _logger.DimseExportInstanceComplete();
                    }
                    else
                    {
                        var errorMessage = $"Failed to export with error {response.Status}";
                        _logger.DimseExportInstanceError(response.Status);
                        exportRequestData.SetFailed(FileExportStatus.ServiceError, errorMessage);
                    }
                    manualResetEvent.Set();
                };

                await client.AddRequestAsync(request).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error while adding DICOM C-STORE request: {exception.Message}";
                _logger.DimseExportErrorAddingInstance(exception.Message, exception);
                exportRequestData.SetFailed(FileExportStatus.ServiceError, errorMessage);
                return false;
            }
        }

        protected async Task ExecuteExport(ExportRequestDataMessage exportRequestData, ManualResetEvent manualResetEvent, DestinationApplicationEntity destination, CancellationToken cancellationToken) => await Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(
                       _configuration.Export.Retries.RetryDelays,
                       (exception, timeSpan, retryCount, context) =>
                       {
                           _logger.DimseExportErrorWithRetry(timeSpan, retryCount, exception);
                       })
                   .ExecuteAsync(async () =>
                   {
                       var client = DicomClientFactory.Create(
                               destination.HostIp,
                               destination.Port,
                               false,
                               _configuration.Dicom.Scu.AeTitle,
                               destination.AeTitle);

                       client.AssociationAccepted += (sender, args) => _logger.ExportAssociationAccepted();
                       client.AssociationRejected += (sender, args) => _logger.ExportAssociationRejected();
                       client.AssociationReleased += (sender, args) => _logger.ExportAssociationReleased();
                       client.ServiceOptions.LogDataPDUs = _configuration.Dicom.Scu.LogDataPdus;
                       client.ServiceOptions.LogDimseDatasets = _configuration.Dicom.Scu.LogDimseDatasets;

                       client.NegotiateAsyncOps();
                       if (await GenerateRequestsAsync(exportRequestData, client, manualResetEvent).ConfigureAwait(false))
                       {
                           _logger.DimseExporting(destination.AeTitle, destination.HostIp, destination.Port);
                           await client.SendAsync(cancellationToken).ConfigureAwait(false);
                           manualResetEvent.WaitOne();
                           _logger.DimseExportComplete(destination.AeTitle);
                       }
                   }).ConfigureAwait(false);

        protected async Task BaseProcessMessage(MessageReceivedEventArgs eventArgs)
        {
            var (exportFlow, reportingActionBlock) = SetupActionBlocks();

            lock (SyncRoot)
            {
                var exportRequest = eventArgs.Message.ConvertTo<ExportRequestEvent>();
                if (ExportRequests.ContainsKey(exportRequest.ExportTaskId))
                {
                    _logger.ExportRequestAlreadyQueued(exportRequest.CorrelationId, exportRequest.ExportTaskId);
                    return;
                }

                exportRequest.MessageId = eventArgs.Message.MessageId;
                exportRequest.DeliveryTag = eventArgs.Message.DeliveryTag;

                var exportRequestWithDetails = new ExportRequestEventDetails(exportRequest);

                ExportRequests.Add(exportRequest.ExportTaskId, exportRequestWithDetails);
                if (!exportFlow.Post(exportRequestWithDetails))
                {
                    _logger.ErrorPostingExportJobToQueue(exportRequest.CorrelationId, exportRequest.ExportTaskId);
                    MessageSubscriber.Reject(eventArgs.Message);
                }
                else
                {
                    _logger.ExportRequestQueuedForProcessing(exportRequest.CorrelationId, exportRequest.MessageId, exportRequest.ExportTaskId);
                }
            }

            exportFlow.Complete();
            await reportingActionBlock.Completion.ConfigureAwait(false);

        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
