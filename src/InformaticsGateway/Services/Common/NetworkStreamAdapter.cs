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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal class NetworkStreamAdapter : INetworkStream
    {
        private readonly NetworkStream _networkStream;
        private bool _disposedValue;

        public int ReadTimeout { get => _networkStream.ReadTimeout; set => _networkStream.ReadTimeout = value; }
        public int WriteTimeout { get => _networkStream.WriteTimeout; set => _networkStream.WriteTimeout = value; }

        public NetworkStreamAdapter(NetworkStream networkStream)
        {
            _networkStream = networkStream ?? throw new ArgumentNullException(nameof(networkStream));
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
            => await _networkStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        public async ValueTask<int> ReadAsync(Memory<byte> messageBuffer, CancellationToken cancellationToken = default)
            => await _networkStream.ReadAsync(messageBuffer, cancellationToken).ConfigureAwait(false);

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> ackData, CancellationToken cancellationToken = default)
            => await _networkStream.WriteAsync(ackData, cancellationToken).ConfigureAwait(false);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _networkStream.Dispose();
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
