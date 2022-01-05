// Copyright 2021 MONAI Consortium
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
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    /// <summary>
    /// An in-memory queue for providing any files/DICOM instances received by the Informatics Gateway to
    /// other internal services.
    /// </summary>
    internal sealed partial class PayloadAssembler : IPayloadAssembler, IDisposable
    {
        internal const int DEFAULT_TIMEOUT = 5;
        private readonly BlockingCollection<Payload> _workItems;
        private readonly ILogger<PayloadAssembler> _logger;
        private ConcurrentDictionary<string, Lazy<Payload>> _payloads;
        private System.Timers.Timer _timer;

        public PayloadAssembler(
            ILogger<PayloadAssembler> logger)
        {
            _workItems = new BlockingCollection<Payload>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _payloads = new ConcurrentDictionary<string, Lazy<Payload>>();

            _timer = new System.Timers.Timer(1000);
            _timer.AutoReset = false;
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
        }

        /// <summary>
        /// Queues a new instance of FileStorageInfo to the bucket with default timeout of 5 seconds.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        public void Queue(string bucket, FileStorageInfo file) => Queue(bucket, file, DEFAULT_TIMEOUT);

        /// <summary>
        /// Queues a new instance of FileStorageInfo.
        /// </summary>
        /// <param name="bucket">Name of the bucket where the file would be added to</param>
        /// <param name="file">Instance to be queued</param>
        /// <param name="timeout">Number of seconds the bucket shall wait before sending the payload to be processed. Note: timeout cannot be modified once the bucket is created.</param>
        public void Queue(string bucket, FileStorageInfo file, uint timeout)
        {
            Guard.Against.Null(file, nameof(file));

            var payload = CreateOrGetPayload(bucket, timeout);
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

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;
                _logger.Log(LogLevel.Trace, $"Number of collections in queue: {_payloads.Count}.");
                foreach (var key in _payloads.Keys)
                {
                    _logger.Log(LogLevel.Trace, $"Checking elapsed time for key: {key}.");
                    var payload = _payloads[key].Value;
                    if (payload.HasTimedOut)
                    {
                        if (payload.Count == 0)
                        {
                            _logger.Log(LogLevel.Warning, $"Something's wrong, found no instances in collection with key={key}");
                            continue;
                        }
                        else
                        {
                            if (_payloads.TryRemove(key, out _))
                            {
                                _workItems.Add(payload);
                                _logger.Log(LogLevel.Information, $"Bucket {key} sent to processing queue.");
                            }
                            else
                            {
                                _logger.Log(LogLevel.Warning, $"Error removing bucket {key} from collection.");
                            }
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
                _logger.Log(LogLevel.Error, ex, "Error scanning timeout collections.");
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private Payload CreateOrGetPayload(string key, uint timeout)
        {
            var payload = _payloads.GetOrAdd(key, x => new Lazy<Payload>(() => new Payload(key, timeout))).Value;
            _logger.Log(LogLevel.Information, $"Bucket {key} created with timeout {timeout}s.");
            return payload;
        }

        public void Dispose()
        {
            _payloads.Clear();
            _timer.Stop();
        }
    }
}