// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal class TcpClientAdapter : ITcpClientAdapter
    {
        private readonly System.Net.Sockets.TcpClient _tcpClient;
        private bool _disposedValue;

        public EndPoint? RemoteEndPoint
        {
            get
            {
                return _tcpClient.Client.RemoteEndPoint;
            }
        }

        public TcpClientAdapter(System.Net.Sockets.TcpClient tcpClient)
            => _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));

        public INetworkStream GetStream()
        {
            return new NetworkStreamAdapter(_tcpClient.GetStream());
        }

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
