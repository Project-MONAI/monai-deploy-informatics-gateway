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
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using DotNext.Threading;

namespace Monai.Deploy.InformaticsGateway.Services.Scu
{
    public class ScuWorkRequest : IDisposable
    {
        private readonly AsyncManualResetEvent _awaiter;
        private ScuWorkResponse _response;
        private bool _disposedValue;

        public string CorrelationId { get; }
        public RequestType RequestType { get; }
        public string HostIp { get; }
        public int Port { get; }
        public string AeTitle { get; }
        public CancellationToken CancellationToken { get; }

        public ScuWorkRequest(string correlationId, RequestType requestType, string hostIp, int port, string aeTitle, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(hostIp, nameof(hostIp));
            Guard.Against.NullOrWhiteSpace(aeTitle, nameof(aeTitle));

            CorrelationId = correlationId;
            RequestType = requestType;
            HostIp = hostIp;
            Port = port;
            AeTitle = aeTitle;
            CancellationToken = cancellationToken;

            _awaiter = new AsyncManualResetEvent(false);
        }

        /// <summary>
        /// Call to complete the request and release the lock.
        /// In case the response is null, a NullResponse is used.
        /// </summary>
        /// <param name="response"></param>
        public void Complete(ScuWorkResponse response)
        {
            response ??= ScuWorkResponse.NullResponse;

            _response = response;
            _awaiter.Set();
        }

        public async Task<ScuWorkResponse> WaitAsync(CancellationToken cancellationToken = default)
        {
            await _awaiter.WaitAsync(cancellationToken).ConfigureAwait(false);
            return _response;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _awaiter.Dispose();
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
