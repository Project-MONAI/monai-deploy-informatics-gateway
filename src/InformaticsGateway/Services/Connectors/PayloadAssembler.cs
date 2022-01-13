// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using DotNext.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            _timer = new System.Timers.Timer(1000);
            _timer.AutoReset = false;
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
        }

        private void RestoreFromDatabase()
        {
            _logger.Log(LogLevel.Information, $"Restoring payloads from database.");
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();

            var payloads = repository.AsQueryable().Where(p => p.State == Payload.PayloadState.Created);
            var restored = 0;
            foreach (var payload in payloads)
            {
                if (_payloads.TryAdd(payload.Key, new AsyncLazy<Payload>(payload)))
                {
                    _logger.Log(LogLevel.Information, $"Payload {payload.Id} restored from database.");
                    restored++;
                }
                else
                {
                    _logger.Log(LogLevel.Information, $"Failed to restore payload {payload.Id} from database.");
                }
            }
            _logger.Log(LogLevel.Information, $"{restored} paylaods restored from database.");
        }

        /// <summary>
        /// Queues a new instance of FileStorageInfo to the bucket with default timeout of 5 seconds.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        public async Task Queue(string bucket, FileStorageInfo file) => await Queue(bucket, file, DEFAULT_TIMEOUT);

        /// <summary>
        /// Queues a new instance of FileStorageInfo.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        /// <param name="timeout">Number of seconds the bucket shall wait before sending the payload to be processed. Note: timeout cannot be modified once the bucket is created.</param>
        public async Task Queue(string bucket, FileStorageInfo file, uint timeout)
        {
            Guard.Against.Null(file, nameof(file));

            var payload = await CreateOrGetPayload(bucket, timeout);
            payload.Add(file);
            _logger.Log(LogLevel.Information, $"File added to bucket {payload.Key}. Queue size: {payload.Count}");
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
                _logger.Log(LogLevel.Trace, $"Number of collections in queue: {_payloads.Count}.");
                foreach (var key in _payloads.Keys)
                {
                    _logger.Log(LogLevel.Trace, $"Checking elapsed time for key: {key}.");
                    var payload = await _payloads[key].Task;
                    if (payload.HasTimedOut)
                    {
                        if (_payloads.TryRemove(key, out _))
                        {
                            if (payload.Files.Count == 0)
                            {
                                _logger.Log(LogLevel.Warning, $"Dropping Bucket {key} due to empty.");
                            }
                            try
                            {
                                payload.State = Payload.PayloadState.Upload;
                                var scope = _serviceScopeFactory.CreateScope();
                                var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                                await payload.UpdatePayload(_options.Value.Storage.Retries.RetryDelays, _logger, repository);
                                _workItems.Add(payload);
                                _logger.Log(LogLevel.Information, $"Bucket {key} sent to processing queue with {payload.Count} files.");
                            }
                            catch (Exception ex)
                            {
                                if (_payloads.TryAdd(key, new AsyncLazy<Payload>(payload)))
                                {
                                    _logger.Log(LogLevel.Warning, ex, $"Error processing payload {payload.Id}, will retry later.");
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            _logger.Log(LogLevel.Warning, $"Error removing bucket {key} from collection.");
                        }
                    }
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.Log(LogLevel.Debug, ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error while processing payload.");
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private async Task<Payload> CreateOrGetPayload(string key, uint timeout)
        {
            return await _payloads.GetOrAdd(key, x => new AsyncLazy<Payload>(async () =>
            {
                var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<Payload>>();
                var newPayload = new Payload(key, timeout);
                await newPayload.AddPayaloadToDatabase(_options.Value.Storage.Retries.RetryDelays, _logger, repository);
                _logger.Log(LogLevel.Information, $"Bucket {key} created with timeout {timeout}s.");
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
