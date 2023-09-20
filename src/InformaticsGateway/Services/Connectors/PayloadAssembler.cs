/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using DotNext.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.Messaging.Events;

#nullable enable

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    /// <summary>
    /// An in-memory queue for providing any files/DICOM instances received by the Informatics Gateway to
    /// other internal services.
    /// </summary>
    internal sealed partial class PayloadAssembler : IPayloadAssembler, IDisposable
    {
        internal const int DEFAULT_TIMEOUT = 5;
        private readonly ILogger<PayloadAssembler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ConcurrentDictionary<string, AsyncLazy<Payload>> _payloads;
        private readonly Task _intializedTask;
        private readonly BlockingCollection<Payload> _workItems;
        private readonly System.Timers.Timer _timer;

        public PayloadAssembler(
            ILogger<PayloadAssembler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            _workItems = new BlockingCollection<Payload>();
            _payloads = new ConcurrentDictionary<string, AsyncLazy<Payload>>();

            _intializedTask = RemovePendingPayloads();

            _timer = new System.Timers.Timer(1000)
            {
                AutoReset = false,
            };
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
        }


        private async Task RemovePendingPayloads()
        {
            _logger.RemovingPendingPayloads();
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPayloadRepository>();

            var removed = await repository.RemovePendingPayloadsAsync().ConfigureAwait(false);

            _logger.TotalNumberOfPayloadsRemoved(removed);
        }

        /// <summary>
        /// Queues a new instance of <see cref="FileStorageMetadata"/> to the bucket with default timeout of 5 seconds.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        /// <param name="dataOrigin">The service that triggered this queue request</param>
        public async Task<Guid> Queue(string bucket, FileStorageMetadata file, DataOrigin dataOrigin) => await Queue(bucket, file, dataOrigin, DEFAULT_TIMEOUT).ConfigureAwait(false);

        /// <summary>
        /// Queues a new instance of <see cref="FileStorageMetadata"/>.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        /// <param name="dataOrigin">The service that triggered this queue request</param>
        /// <param name="timeout">Number of seconds the bucket shall wait before sending the payload to be processed. Note: timeout cannot be modified once the bucket is created.</param>
        public async Task<Guid> Queue(string bucket, FileStorageMetadata file, DataOrigin dataOrigin, uint timeout)
        {
            Guard.Against.Null(file, nameof(file));

            await _intializedTask.ConfigureAwait(false);

            using var _ = _logger.BeginScope(new LoggingDataDictionary<string, object>() { { "CorrelationId", file.CorrelationId } });

            var payload = await CreateOrGetPayload(bucket, file.CorrelationId, file.WorkflowInstanceId, file.TaskId, dataOrigin, timeout).ConfigureAwait(false);
            payload.Add(file);
            _logger.FileAddedToBucket(payload.Key, payload.Count);
            return payload.PayloadId;
        }

        /// <summary>
        /// Dequeued a payload if available; otherwise, the call is blocked until an instance is available
        /// or when cancellation token is set.
        /// </summary>
        /// <param name="cancellationToken">Instance of cancellation token</param>
        /// <returns>Instance of Payload</returns>
        public Payload Dequeue(CancellationToken cancellationToken)
        {
            return _workItems.Take(cancellationToken);
        }

        private async void OnTimedEvent(object? source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                await _intializedTask.ConfigureAwait(false);

                _timer.Enabled = false;
                if (!_payloads.IsEmpty)
                {
                    _logger.BucketsActive(_payloads.Count);
                }
                foreach (var key in _payloads.Keys)
                {
                    var payload = await _payloads[key].Task.ConfigureAwait(false);
                    using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "CorrelationId", payload.CorrelationId } });

                    _logger.BucketElapsedTime(key, payload.Timeout, payload.ElapsedTime().TotalSeconds, payload.Files.Count, payload.FilesUploaded, payload.FilesFailedToUpload);
                    // Wait for timer window closes before sending payload for processing
                    if (payload.HasTimedOut)
                    {
                        if (payload.IsUploadCompleted())
                        {
                            if (_payloads.TryRemove(key, out _))
                            {
                                await QueueBucketForNotification(key, payload).ConfigureAwait(false);
                            }
                            else
                            {
                                _logger.BucketRemoveError(key);
                            }
                        }
                        else if (payload.IsUploadCompletedWithFailures())
                        {
                            _payloads.TryRemove(key, out _);
                            _logger.PayloadRemovedWithFailureUploads(key, payload.Count, payload.Files.Count(p => p.IsUploadFailed));
                        }
                    }
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.BucketNotFound(ex);
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingBuckets(ex);
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private async Task QueueBucketForNotification(string key, Payload payload)
        {
            try
            {
                payload.State = Payload.PayloadState.Move;
                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IPayloadRepository>();
                await repository.UpdateAsync(payload).ConfigureAwait(false);
                _logger.PayloadSaved(payload.PayloadId);
                _workItems.Add(payload);
                _logger.BucketReady(key, payload.Count);
            }
            catch (Exception ex)
            {
                if (_payloads.TryAdd(key, new AsyncLazy<Payload>(payload)))
                {
                    _logger.BucketError(key, payload.PayloadId, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task<Payload> CreateOrGetPayload(string key, string correlationId, string? workflowInstanceId, string? taskId, Messaging.Events.DataOrigin dataOrigin, uint timeout)
        {
            return await _payloads.GetOrAdd(key, x => new AsyncLazy<Payload>(async () =>
            {
                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IPayloadRepository>();
                var newPayload = new Payload(key, correlationId, workflowInstanceId, taskId, dataOrigin, timeout);
                await repository.AddAsync(newPayload).ConfigureAwait(false);
                _logger.BucketCreated(key, timeout);
                return newPayload;
            }));
        }

        public void Dispose()
        {
            _payloads.Clear();
            _timer.Stop();
        }
    }
}
