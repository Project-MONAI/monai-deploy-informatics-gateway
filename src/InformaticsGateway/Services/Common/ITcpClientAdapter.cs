// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal interface ITcpClientAdapter : IDisposable
    {
        EndPoint RemoteEndPoint { get; }

        INetworkStream GetStream();
    }
}
