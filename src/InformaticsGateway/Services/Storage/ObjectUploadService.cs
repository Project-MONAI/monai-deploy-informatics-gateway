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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.Storage.API;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class ObjectUploadService : IHostedService, IMonaiService, IDisposable
    {
        private readonly ILogger<ObjectUploadService> _logger;
        private readonly IObjectUploadQueue _uplaodQueue;
        private readonly IStorageService _storageService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IServiceScope _scope;
        private bool _disposedValue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public string ServiceName => "Object Upload Service";

        public ObjectUploadService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ObjectUploadService> logger,
            IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _scope = _serviceScopeFactory.CreateScope();
            _uplaodQueue = _scope.ServiceProvider.GetService<IObjectUploadQueue>() ?? throw new ServiceNotFoundException(nameof(IObjectUploadQueue));
            _storageService = _scope.ServiceProvider.GetService<IStorageService>() ?? throw new ServiceNotFoundException(nameof(IStorageService));
        }

        private void BackgroundProcessingAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceRunning(ServiceName);
            var tasks = new List<Task>();
            try
            {
                _logger.InitializeThreads(_configuration.Value.Storage.ConcurrentUploads);
                for (var i = 0; i < _configuration.Value.Storage.ConcurrentUploads; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await StartWorker(i, _cancellationTokenSource.Token).ConfigureAwait(false);
                    }, cancellationToken));
                }

                Task.WaitAll(tasks.ToArray(), _cancellationTokenSource.Token);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.ServiceDisposed(ServiceName, ex);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException || ex is OperationCanceledException)
                {
                    _logger.ServiceInvalidOrCancelled(ServiceName, ex);
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        private async Task StartWorker(int thread, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var item = await _uplaodQueue.Dequeue(cancellationToken).ConfigureAwait(false);
                    await ProcessObject(thread, item).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.ServiceCancelledWithException(ServiceName, ex);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorUploading(ex);
                }
            }
            Status = ServiceStatus.Stopped;
            _logger.ServiceStopping(ServiceName);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                BackgroundProcessingAsync(cancellationToken);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceRunning(ServiceName);
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            _cancellationTokenSource.Cancel();
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private async Task ProcessObject(int thread, FileStorageMetadata blob)
        {
            Guard.Against.Null(blob, nameof(blob));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "Thread", thread }, { "File ID", blob.Id }, { "CorrelationId", blob.CorrelationId } });
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();

                switch (blob)
                {
                    case DicomFileStorageMetadata dicom:
                        if (!string.IsNullOrWhiteSpace(dicom.JsonFile.TemporaryPath))
                        {
                            await UploadFileAndConfirm(dicom.Id, dicom.JsonFile, dicom.DataOrigin.Source, dicom.Workflows, blob.PayloadId, _cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        break;
                }

                await UploadFileAndConfirm(blob.Id, blob.File, blob.DataOrigin.Source, blob.Workflows, blob.PayloadId, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                blob.SetFailed();
                _logger.FailedToUploadFile(blob.Id, ex);
            }
            finally
            {
                stopwatch.Stop();
                _logger.UploadStats(_configuration.Value.Storage.ConcurrentUploads, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private async Task UploadFileAndConfirm(string identifier, StorageObjectMetadata storageObjectMetadata, string source, List<string> workflows, string payloadId, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));
            Guard.Against.Null(storageObjectMetadata, nameof(storageObjectMetadata));
            Guard.Against.NullOrWhiteSpace(source, nameof(source));
            Guard.Against.Null(workflows, nameof(workflows));

            if (storageObjectMetadata.IsUploaded)
            {
                return;
            }

            await UploadFile(storageObjectMetadata, source, workflows, payloadId, cancellationToken).ConfigureAwait(false);
            var count = 3;
            while (
                count-- > 0 &&
                !await VerifyExists(storageObjectMetadata.GetPayloadPath(Guid.Parse(payloadId)), cancellationToken).ConfigureAwait(false)
                ) { /* no op */};

            if (count <= 0)
            {
                throw new FileUploadException($"Failed to upload file after retries {identifier}.");
            }
        }

        private async Task<bool> VerifyExists(string path, CancellationToken cancellationToken)
        {
            return await Policy
               .Handle<VerifyObjectsException>()
               .WaitAndRetryAsync(
                   _configuration.Value.Storage.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorUploadingFileToTemporaryStore(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   var internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                   internalCancellationTokenSource.CancelAfter(_configuration.Value.Storage.StorageServiceListTimeout);
                   var exists = await _storageService.VerifyObjectExistsAsync(_configuration.Value.Storage.StorageServiceBucketName, path).ConfigureAwait(false);
                   _logger.VerifyFileExists(path, exists);
                   return exists;
               })
               .ConfigureAwait(false);
        }

        private async Task UploadFile(StorageObjectMetadata storageObjectMetadata, string source, List<string> workflows, string payloadId, CancellationToken cancellationToken)
        {
            _logger.UploadingFileToStoreage(storageObjectMetadata.TemporaryPath);
            var metadata = new Dictionary<string, string>
                {
                    { FileMetadataKeys.Source, source },
                    { FileMetadataKeys.Workflows, workflows.IsNullOrEmpty() ? string.Empty : string.Join(',', workflows) }
                };

            await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   _configuration.Value.Storage.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorUploadingFileToTemporaryStore(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   if (storageObjectMetadata.IsUploaded) { return; }

                   var bucket = _configuration.Value.Storage.StorageServiceBucketName;
                   var path = storageObjectMetadata.GetPayloadPath(Guid.Parse(payloadId));

                   storageObjectMetadata.Data.Seek(0, System.IO.SeekOrigin.Begin);
                   await _storageService.PutObjectAsync(
                       bucket,
                       path,
                       storageObjectMetadata.Data,
                       storageObjectMetadata.Data.Length,
                       storageObjectMetadata.ContentType,
                       metadata,
                       cancellationToken).ConfigureAwait(false);
                   storageObjectMetadata.SetUploaded(_configuration.Value.Storage.TemporaryStorageBucket); // deletes local file and sets uploaded to true
                   _logger.UploadedFileToStoreage(storageObjectMetadata.TemporaryPath);
                   storageObjectMetadata.SetMoved(_configuration.Value.Storage.StorageServiceBucketName); // set bucket, date moved, and move complete
               })
               .ConfigureAwait(false);
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
