/*
 * Copyright 2023 MONAI Consortium
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ardalis.GuardClauses;
using DotNext.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.Storage.API;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    internal interface IPayloadMoveActionHandler
    {
        Task MoveFilesAsync(Payload payload, ActionBlock<Payload> moveQueue, ActionBlock<Payload> notificationQueue, CancellationToken cancellationToken = default);
    }

    internal class PayloadMoveActionHandler : IPayloadMoveActionHandler, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PayloadMoveActionHandler> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly IServiceScope _scope;
        private readonly IStorageService _storageService;
        private bool _disposedValue;

        public PayloadMoveActionHandler(IServiceScopeFactory serviceScopeFactory,
                                        ILogger<PayloadMoveActionHandler> logger,
                                        IOptions<InformaticsGatewayConfiguration> options)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _scope = _serviceScopeFactory.CreateScope();
            _storageService = _scope.ServiceProvider.GetService<IStorageService>() ?? throw new ServiceNotFoundException(nameof(IStorageService));
        }

        public async Task MoveFilesAsync(Payload payload, ActionBlock<Payload> moveQueue, ActionBlock<Payload> notificationQueue, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.Null(moveQueue, nameof(moveQueue));
            Guard.Against.Null(notificationQueue, nameof(notificationQueue));

            if (payload.State != Payload.PayloadState.Move)
            {
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.IncorrectState, false);
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await NotifyIfCompleted(payload, notificationQueue, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                payload.RetryCount++;
                var action = await UpdatePayloadState(payload, ex, cancellationToken).ConfigureAwait(false);
                if (action == PayloadAction.Updated)
                {
                    if (!await moveQueue.Post(payload, _options.Value.Storage.Retries.RetryDelays.ElementAt(payload.RetryCount - 1)).ConfigureAwait(false))
                    {
                        throw new PostPayloadException(Payload.PayloadState.Move, payload);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
                _logger.CopyStats(_options.Value.Storage.ConcurrentUploads, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private async Task NotifyIfCompleted(Payload payload, ActionBlock<Payload> notificationQueue, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.Null(notificationQueue, nameof(notificationQueue));

            if (payload.IsMoveCompleted())
            {
                payload.State = Payload.PayloadState.Notify;
                payload.ResetRetry();

                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetService<IPayloadRepository>() ?? throw new ServiceNotFoundException(nameof(IPayloadRepository));
                await repository.UpdateAsync(payload, cancellationToken).ConfigureAwait(false);
                _logger.PayloadSaved(payload.PayloadId);

                if (!notificationQueue.Post(payload))
                {
                    throw new PostPayloadException(Payload.PayloadState.Notify, payload);
                }

                _logger.PayloadReadyToBePublished(payload.PayloadId);
            }
            else // we should never hit this else block.
            {
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.IncompletePayload, false);
            }
        }

        private async Task<PayloadAction> UpdatePayloadState(Payload payload, Exception ex, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload, nameof(payload));

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPayloadRepository>() ?? throw new ServiceNotFoundException(nameof(IPayloadRepository));

            try
            {
                if (ex is AggregateException aggregateException &&
                    aggregateException.InnerExceptions.Any(p => (p is PayloadNotifyException payloadNotifyEx) && payloadNotifyEx.ShallRetry == false))
                {
                    _logger.DeletePayloadDueToMissingFiles(payload.PayloadId, ex);
                    await repository.RemoveAsync(payload, cancellationToken).ConfigureAwait(false);
                    _logger.PayloadDeleted(payload.PayloadId);
                    return PayloadAction.Deleted;
                }
                else if (payload.RetryCount > _options.Value.Storage.Retries.DelaysMilliseconds.Length)
                {
                    _logger.MoveFailureStopRetry(payload.PayloadId, ex);
                    await repository.RemoveAsync(payload, cancellationToken).ConfigureAwait(false);
                    _logger.PayloadDeleted(payload.PayloadId);
                    return PayloadAction.Deleted;
                }
                else
                {
                    _logger.MoveFailureRetryLater(payload.PayloadId, payload.State, payload.RetryCount, ex);
                    await repository.UpdateAsync(payload, cancellationToken).ConfigureAwait(false);
                    _logger.PayloadSaved(payload.PayloadId);
                    return PayloadAction.Updated;
                }
            }
            catch (Exception iex)
            {
                _logger.ErrorUpdatingPayload(payload.PayloadId, iex);
                return PayloadAction.Updated;
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
