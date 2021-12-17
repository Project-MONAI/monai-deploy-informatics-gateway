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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    /// <summary>
    /// An in-memory queue for providing any files/DICOM instances received by the Informatics Gateway to
    /// other internal services.
    /// </summary>
    internal sealed partial class PayloadAssembler : IPayloadAssembler
    {
        private const int DEFAULT_TIMEOUT = 5;
        private static readonly object SyncRoot = new object();
        private readonly BlockingCollection<Payload> _workItems;
        private readonly ILogger<PayloadAssembler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Dictionary<string, Payload> _payloads;
        private System.Timers.Timer _timer;

        public PayloadAssembler(
            ILogger<PayloadAssembler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _workItems = new BlockingCollection<Payload>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _payloads = new Dictionary<string, Payload>();

            _timer = new System.Timers.Timer(1000);
            _timer.AutoReset = false;
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
        }


        /// <summary>
        /// Queues a new instance of FileStorageInfo to the bucket with default timeout of 5 seconds.
        /// </summary>
        /// <param name="file">Instance to be queued</param>
        public void Queue(string bucket, FileStorageInfo file) => Queue(bucket, file, DEFAULT_TIMEOUT);


        /// <summary>
        /// Queues a new instance of FileStorageInfo.
        /// </summary>
        /// <param name="file">Instance to be queued</param>
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
                    var payload = _payloads[key];
                    lock (SyncRoot)
                    {
                        if (payload.HasTimedOut)
                        {
                            if (payload.Count == 0)
                            {
                                _logger.Log(LogLevel.Warning, $"Something's wrong, found no instances in collection with key={key}");
                                continue;
                            }
                            else
                            {
                                _workItems.Add(payload);
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
                _logger.Log(LogLevel.Error, ex, "Error scanning collection for timeout collections.");
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private Payload CreateOrGetPayload(string key, uint timeout)
        {
            lock (SyncRoot)
            {
                if (!_payloads.ContainsKey(key))
                {
                    _payloads.Add(key, new Payload(key, timeout));
                }
                return _payloads[key];
            }
        }
    }
}
