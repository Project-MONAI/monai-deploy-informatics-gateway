// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Net;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal interface ITcpListenerFactory
    {
        ITcpListener CreateTcpListener(IPAddress ipaddress, int port);
    }

    internal class TcpListenerFactory : ITcpListenerFactory
    {
        public ITcpListener CreateTcpListener(IPAddress ipaddress, int port) => new TcpListener(ipaddress, port);
    }
}
