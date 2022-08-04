// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal interface IMllpClient
    {
        Guid ClientId { get; }

        void Dispose();

        Task Start(Action<IMllpClient, MllpClientResult> onDisconnect, CancellationToken cancellationToken);
    }
}
