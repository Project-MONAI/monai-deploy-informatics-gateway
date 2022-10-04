/*
 * Copyright 2022 MONAI Consortium
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
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
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.IncorrectState);
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await Move(payload, cancellationToken).ConfigureAwait(false);
                await NotifyIfCompleted(payload, notificationQueue).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                payload.RetryCount++;
                var action = await UpdatePayloadState(payload, ex).ConfigureAwait(false);
                if (action == PayloadAction.Updated)
                {
                    await moveQueue.Post(payload, _options.Value.Storage.Retries.RetryDelays.ElementAt(payload.RetryCount - 1)).ConfigureAwait(false);
                }
            }
            finally
            {
                stopwatch.Stop();
                _logger.CopyStats(_options.Value.Storage.ConcurrentUploads, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private async Task NotifyIfCompleted(Payload payload, ActionBlock<Payload> notificationQueue)
        {
            Guard.Against.Null(payload, nameof(payload));
            Guard.Against.Null(notificationQueue, nameof(notificationQueue));

            if (payload.IsMoveCompleted())
            {
                payload.State = Payload.PayloadState.Notify;
                payload.ResetRetry();

                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetService<IInformaticsGatewayRepository<Payload>>() ?? throw new ServiceNotFoundException(nameof(IInformaticsGatewayRepository<Payload>));
                await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);

                notificationQueue.Post(payload);
                _logger.PayloadReadyToBePublished(payload.Id);
            }
            else // we should never hit this else block.
            {
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.IncompletePayload);
            }
        }

        private async Task Move(Payload payload, CancellationToken cancellationToken)
        {
            Guard.Against.Null(payload, nameof(payload));

            _logger.MovingFIlesInPayload(payload.Id, _options.Value.Storage.StorageServiceBucketName);

            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _options.Value.Storage.ConcurrentUploads
            };

            await Parallel.ForEachAsync(payload.Files, options, async (file, cancellationToke) =>
            {
                switch (file)
                {
                    case DicomFileStorageMetadata dicom:
                        if (!string.IsNullOrWhiteSpace(dicom.JsonFile.TemporaryPath))
                        {
                            await MoveFile(payload.Id, dicom.Id, dicom.JsonFile, cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }

                await MoveFile(payload.Id, file.Id, file.File, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task MoveFile(Guid payloadId, string identity, StorageObjectMetadata file, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(identity, nameof(identity));
            Guard.Against.Null(file, nameof(file));

            _logger.MovingFileToPayloadDirectory(payloadId, identity);
            await _storageService.CopyObjectAsync(
                file.TemporaryBucketName,
                file.GetTempStoragPath(_options.Value.Storage.RemoteTemporaryStoragePath),
                _options.Value.Storage.StorageServiceBucketName,
                file.GetPayloadPath(payloadId),
                cancellationToken).ConfigureAwait(false);

            _logger.DeletingFileFromTemporaryBbucket(file.TemporaryBucketName, identity, file.TemporaryPath);
            await _storageService.RemoveObjectAsync(file.TemporaryBucketName, file.GetTempStoragPath(_options.Value.Storage.RemoteTemporaryStoragePath), cancellationToken);

            file.SetMoved(_options.Value.Storage.StorageServiceBucketName);
        }

        private async Task<PayloadAction> UpdatePayloadState(Payload payload, Exception ex)
        {
            Guard.Against.Null(payload, nameof(payload));

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IInformaticsGatewayRepository<Payload>>() ?? throw new ServiceNotFoundException(nameof(IInformaticsGatewayRepository<Payload>));

            try
            {
                if (payload.RetryCount > _options.Value.Storage.Retries.DelaysMilliseconds.Length)
                {
                    _logger.MoveFailureStopRetry(payload.Id, ex);
                    await payload.DeletePayload(_options.Value.Database.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
                    return PayloadAction.Deleted;
                }
                else
                {
                    _logger.MoveFailureRetryLater(payload.Id, payload.State, payload.RetryCount, ex);
                    await payload.UpdatePayload(_options.Value.Database.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
                    return PayloadAction.Updated;
                }
            }
            catch (Exception iex)
            {
                _logger.ErrorUpdatingPayload(payload.Id, iex);
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
