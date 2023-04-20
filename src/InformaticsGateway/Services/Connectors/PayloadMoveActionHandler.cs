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
using System.Collections.Generic;
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
using Polly;

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
            Guard.Against.Null(payload);
            Guard.Against.Null(moveQueue);
            Guard.Against.Null(notificationQueue);

            if (payload.State != Payload.PayloadState.Move)
            {
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.IncorrectState, false);
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await Move(payload, cancellationToken).ConfigureAwait(false);
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
            Guard.Against.Null(payload);
            Guard.Against.Null(notificationQueue);

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

        private async Task Move(Payload payload, CancellationToken cancellationToken)
        {
            Guard.Against.Null(payload);

            _logger.MovingFIlesInPayload(payload.PayloadId, _options.Value.Storage.StorageServiceBucketName);

            var options = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = _options.Value.Storage.ConcurrentUploads
            };

            var exceptions = new List<Exception>();
            await Parallel.ForEachAsync(payload.Files, options, async (file, cancellationToke) =>
            {
                try
                {
                    switch (file)
                    {
                        case DicomFileStorageMetadata dicom:
                            if (!string.IsNullOrWhiteSpace(dicom.JsonFile.TemporaryPath))
                            {
                                await MoveFile(payload.PayloadId, dicom.Id, dicom.JsonFile, cancellationToken).ConfigureAwait(false);
                            }
                            break;
                    }

                    await MoveFile(payload.PayloadId, file.Id, file.File, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }).ConfigureAwait(false);

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private async Task MoveFile(Guid payloadId, string identity, StorageObjectMetadata file, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(identity);
            Guard.Against.Null(file);

            if (file.IsMoveCompleted)
            {
                _logger.AlreadyMoved(payloadId, file.UploadPath);
                return;
            }

            _logger.MovingFileToPayloadDirectory(payloadId, file.UploadPath);

            try
            {
                await _storageService.CopyObjectAsync(
                    file.TemporaryBucketName,
                    file.GetTempStoragPath(_options.Value.Storage.RemoteTemporaryStoragePath),
                    _options.Value.Storage.StorageServiceBucketName,
                    file.GetPayloadPath(payloadId),
                    cancellationToken).ConfigureAwait(false);

                await VerifyFileExists(payloadId, file, cancellationToken).ConfigureAwait(false);
            }
            catch (StorageObjectNotFoundException ex) when (ex.Message.Contains("Not found", StringComparison.OrdinalIgnoreCase)) // TODO: StorageLib shall not throw any errors from MINIO
            {
                // when file cannot be found on the Storage Service, we assume file has been moved previously by verifying the file exists on destination.
                _logger.FileMissingInPayload(payloadId, file.GetTempStoragPath(_options.Value.Storage.RemoteTemporaryStoragePath), ex);
                await VerifyFileExists(payloadId, file, cancellationToken).ConfigureAwait(false);
            }
            catch (StorageConnectionException ex)
            {
                _logger.StorageServiceConnectionError(ex);
                throw new PayloadNotifyException(PayloadNotifyException.FailureReason.ServiceUnavailable);
            }
            catch (Exception ex)
            {
                _logger.PayloadMoveException(ex);
                await LogFilesInMinIo(file.TemporaryBucketName, cancellationToken).ConfigureAwait(false);
                throw new FileMoveException(file.GetTempStoragPath(_options.Value.Storage.RemoteTemporaryStoragePath), file.UploadPath, ex);
            }

            try
            {
                _logger.DeletingFileFromTemporaryBbucket(file.TemporaryBucketName, identity, file.TemporaryPath);
                await _storageService.RemoveObjectAsync(file.TemporaryBucketName, file.GetTempStoragPath(_options.Value.Storage.RemoteTemporaryStoragePath), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                _logger.ErrorDeletingFileAfterMoveComplete(file.TemporaryBucketName, identity, file.TemporaryPath);
            }
            finally
            {
                file.SetMoved(_options.Value.Storage.StorageServiceBucketName);
            }
        }

        private async Task VerifyFileExists(Guid payloadId, StorageObjectMetadata file, CancellationToken cancellationToken)
        {
            await Policy
               .Handle<VerifyObjectsException>()
               .WaitAndRetryAsync(
                   _options.Value.Storage.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorUploadingFileToTemporaryStore(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   var internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                   internalCancellationTokenSource.CancelAfter(_options.Value.Storage.StorageServiceListTimeout);
                   var exists = await _storageService.VerifyObjectExistsAsync(_options.Value.Storage.StorageServiceBucketName, file.GetPayloadPath(payloadId), cancellationToken).ConfigureAwait(false);
                   if (!exists)
                   {
                       _logger.FileMovedVerificationFailure(payloadId, file.UploadPath);
                       throw new PayloadNotifyException(PayloadNotifyException.FailureReason.MoveFailure, false);
                   }
               })
               .ConfigureAwait(false);
        }

        private async Task LogFilesInMinIo(string bucketName, CancellationToken cancellationToken)
        {
            try
            {
                var internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                internalCancellationTokenSource.CancelAfter(_options.Value.Storage.StorageServiceListTimeout);
                var listingResults = await _storageService.ListObjectsAsync(bucketName, recursive: true, cancellationToken: internalCancellationTokenSource.Token).ConfigureAwait(false);
                _logger.FilesFounddOnStorageService(bucketName, listingResults.Count);
                var files = new List<string>();
                foreach (var item in listingResults)
                {
                    files.Add(item.FilePath);
                }
                _logger.FileFounddOnStorageService(bucketName, string.Join(Environment.NewLine, files));
            }
            catch (Exception ex)
            {
                _logger.ErrorListingFilesOnStorageService(ex);
            }
        }

        private async Task<PayloadAction> UpdatePayloadState(Payload payload, Exception ex, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(payload);

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
