/*
 * Copyright 2021-2022 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    internal enum PayloadAction
    {
        Updated,
        Deleted
    }

    /// <summary>
    /// The Payload Notification Service class includes 2 action blocks (queues) that process
    /// any payloads that are in either <c>Payload.PayloadState.Upload</c> or
    /// <c>Payload.PayloadState.Notify</c> states. It also queries the payload assembler for
    /// payloads that are ready for upload in the background.
    /// </summary>
    internal class PayloadNotificationService : IHostedService, IMonaiService, IDisposable
    {
        /// <summary>
        /// Internal use to indicate if payload was updated or deleted.
        /// </summary>

        private static readonly Payload.PayloadState[] SupportedStates = new[] { Payload.PayloadState.Move, Payload.PayloadState.Notify };

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PayloadNotificationService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IServiceScope _scope;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IPayloadNotificationActionHandler _payloadNotificationActionHandler;
        private readonly IPayloadMoveActionHandler _payloadMoveActionHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private ActionBlock<Payload> _moveFileQueue;
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

            _payloadAssembler = _scope.ServiceProvider.GetService<IPayloadAssembler>() ?? throw new ServiceNotFoundException(nameof(IPayloadAssembler));

            _payloadNotificationActionHandler = _scope.ServiceProvider.GetService<IPayloadNotificationActionHandler>() ?? throw new ServiceNotFoundException(nameof(IPayloadNotificationActionHandler));
            _payloadMoveActionHandler = _scope.ServiceProvider.GetService<IPayloadMoveActionHandler>() ?? throw new ServiceNotFoundException(nameof(IPayloadMoveActionHandler));

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _moveFileQueue = new ActionBlock<Payload>(
                    MoveActionHandler,
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = _options.Value.Storage.PayloadProcessThreads,
                        MaxMessagesPerTask = 1,
                        CancellationToken = cancellationToken
                    });

            _publishQueue = new ActionBlock<Payload>(
                    NotificationHandler,
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

        private async Task NotificationHandler(Payload payload)
        {
            Guard.Against.Null(payload, nameof(payload));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "Payload", payload.Id }, { "Correlation ID", payload.CorrelationId } });

            try
            {
                await _payloadNotificationActionHandler.NotifyAsync(payload, _publishQueue, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is PayloadNotifyException payloadMoveException &&
                    payloadMoveException.Reason == PayloadNotifyException.FailureReason.IncorrectState)
                {
                    _logger.FailedToMoveFilesInPayloadIncorrectState(payload.State, ex);
                }
                else
                {
                    _logger.FailedToMoveFilesInPayloadUknownError(ex);
                }
            }
        }

        private async Task MoveActionHandler(Payload payload)
        {
            Guard.Against.Null(payload, nameof(payload));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "Payload", payload.Id }, { "Correlation ID", payload.CorrelationId } });

            try
            {
                await _payloadMoveActionHandler.MoveFilesAsync(payload, _moveFileQueue, _publishQueue, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is PayloadNotifyException payloadMoveException &&
                    payloadMoveException.Reason == PayloadNotifyException.FailureReason.IncorrectState)
                {
                    _logger.FailedToMoveFilesInPayloadIncorrectState(payload.State, ex);
                }
                else
                {
                    _logger.FailedToMoveFilesInPayloadUknownError(ex);
                }
            }
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
                        _moveFileQueue.Post(payload);
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

        private void RestoreFromDatabase()
        {
            _logger.StartupRestoreFromDatabase();

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IInformaticsGatewayRepository<Payload>>() ?? throw new ServiceNotFoundException(nameof(IInformaticsGatewayRepository<Payload>));

            var payloads = repository.AsQueryable().Where(p => SupportedStates.Contains(p.State));
            foreach (var payload in payloads)
            {
                if (payload.State == Payload.PayloadState.Move)
                {
                    _moveFileQueue.Post(payload);
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
            _cancellationTokenSource.Cancel();
            _moveFileQueue.Complete();
            _publishQueue.Complete();
            Status = ServiceStatus.Stopped;

            _logger.ServiceStopPending(ServiceName);

            await Task.WhenAll(_moveFileQueue.Completion, _publishQueue.Completion).ConfigureAwait(false);
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
