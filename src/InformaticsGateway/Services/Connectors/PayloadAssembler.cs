// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    /// <summary>
    /// An in-memory queue for providing any files/DICOM instances received by the Informatics Gateway to
    /// other internal services.
    /// </summary>
    internal sealed partial class PayloadAssembler : IPayloadAssembler, IDisposable
    {
        internal const int DEFAULT_TIMEOUT = 5;
        private readonly BlockingCollection<Payload> _workItems;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly ILogger<PayloadAssembler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ConcurrentDictionary<string, AsyncLazy<Payload>> _payloads;
        private readonly System.Timers.Timer _timer;

        public PayloadAssembler(
            IOptions<InformaticsGatewayConfiguration> options,
            ILogger<PayloadAssembler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            _workItems = new BlockingCollection<Payload>();
            _payloads = new ConcurrentDictionary<string, AsyncLazy<Payload>>();

            RestoreFromDatabase();

            _timer = new System.Timers.Timer(1000)
            {
                AutoReset = false,
            };
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
        }

        private void RestoreFromDatabase()
        {
            _logger.RestorePayloads();
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();

            var payloads = repository.AsQueryable().Where(p => p.State == Payload.PayloadState.Created);
            var restored = 0;
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
            foreach (var payload in payloads)
            {
                if (_payloads.TryAdd(payload.Key, new AsyncLazy<Payload>(payload)))
                {
                    _logger.PayloadRestored(payload.Id);
                    restored++;
                }
            }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions

            _logger.TotalNumberOfPayloadsRestored(restored);
        }

        /// <summary>
        /// Queues a new instance of FileStorageInfo to the bucket with default timeout of 5 seconds.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        public async Task Queue(string bucket, FileStorageInfo file) => await Queue(bucket, file, DEFAULT_TIMEOUT).ConfigureAwait(false);

        /// <summary>
        /// Queues a new instance of FileStorageInfo.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        /// <param name="timeout">Number of seconds the bucket shall wait before sending the payload to be processed. Note: timeout cannot be modified once the bucket is created.</param>
        public async Task Queue(string bucket, FileStorageInfo file, uint timeout)
        {
            Guard.Against.Null(file, nameof(file));

            using var _ = _logger.BeginScope(new LoggingDataDictionary<string, object>() { { "Correlation ID", file.CorrelationId } });

            var payload = await CreateOrGetPayload(bucket, file.CorrelationId, timeout).ConfigureAwait(false);
            payload.Add(file);
            _logger.FileAddedToBucket(payload.Key, payload.Count);
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

        private async void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;
                _logger.BucketActive(_payloads.Count);
                foreach (var key in _payloads.Keys)
                {
                    _logger.BucketElapsedTime(key);
                    var payload = await _payloads[key].Task.ConfigureAwait(false);
                    if (payload.HasTimedOut)
                    {
                        if (_payloads.TryRemove(key, out _))
                        {
                            if (payload.Files.Count == 0)
                            {
                                _logger.DropEmptyBucket(key);
                                return;
                            }

                            await QueueBucketForNotification(key, payload).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger.BucketRemoveError(key);
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
                payload.State = Payload.PayloadState.Upload;
                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
                _workItems.Add(payload);
                _logger.BucketReady(key, payload.Count);
            }
            catch (Exception ex)
            {
                if (_payloads.TryAdd(key, new AsyncLazy<Payload>(payload)))
                {
                    _logger.BucketError(key, payload.Id, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task<Payload> CreateOrGetPayload(string key, string correationId, uint timeout)
        {
            return await _payloads.GetOrAdd(key, x => new AsyncLazy<Payload>(async () =>
            {
                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                var newPayload = new Payload(key, correationId, timeout);
                await newPayload.AddPayaloadToDatabase(_options.Value.Storage.Retries.RetryDelays, _logger, repository).ConfigureAwait(false);
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
