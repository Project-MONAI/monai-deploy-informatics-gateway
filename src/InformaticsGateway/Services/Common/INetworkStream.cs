// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    internal interface INetworkStream : IDisposable
    {
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }

        ValueTask WriteAsync(ReadOnlyMemory<byte> ackData, CancellationToken cancellationToken = default);

        Task FlushAsync(CancellationToken cancellationToken = default);

        ValueTask<int> ReadAsync(Memory<byte> messageBuffer, CancellationToken cancellationToken = default);
    }
}
