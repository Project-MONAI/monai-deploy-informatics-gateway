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
using System.Net;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal class TcpClientAdapter : ITcpClientAdapter
    {
        private readonly System.Net.Sockets.TcpClient _tcpClient;
        private bool _disposedValue;

        public EndPoint RemoteEndPoint
        {
            get
            {
                return _tcpClient.Client.RemoteEndPoint;
            }
        }

        public TcpClientAdapter(System.Net.Sockets.TcpClient tcpClient)
            => _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));

        public INetworkStream GetStream() => new NetworkStreamAdapter(_tcpClient.GetStream());

        public void Close() => _tcpClient.Close();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _tcpClient.Dispose();
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
