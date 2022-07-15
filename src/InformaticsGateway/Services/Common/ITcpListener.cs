// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
