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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal interface ITcpListener
    {
        void Start();

        void Stop();

        ValueTask<ITcpClientAdapter> AcceptTcpClientAsync(CancellationToken cancellationToken = default);
    }

    internal class TcpListener : ITcpListener
    {
        private readonly System.Net.Sockets.TcpListener _tcpListener;

        public TcpListener(IPAddress ipAddress, int port)
        {
            Guard.Against.Null(ipAddress, nameof(ipAddress));

            _tcpListener = new System.Net.Sockets.TcpListener(ipAddress, port);
        }

        public void Start()
        {
            _tcpListener.Start();
        }

        public void Stop()
        {
            _tcpListener.Stop();
        }

        public async ValueTask<ITcpClientAdapter> AcceptTcpClientAsync(CancellationToken cancellationToken = default)
        {
            return new TcpClientAdapter(await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
