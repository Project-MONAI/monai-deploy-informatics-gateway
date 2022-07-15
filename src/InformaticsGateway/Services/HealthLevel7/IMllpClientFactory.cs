// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal interface IMllpClientFactory
    {
        IMllpClient CreateClient(ITcpClientAdapter client, Hl7Configuration configurations, ILogger<MllpClient> logger);
    }

    internal class MllpClientFactory : IMllpClientFactory
    {
        public IMllpClient CreateClient(ITcpClientAdapter client, Hl7Configuration configurations, ILogger<MllpClient> logger) => new MllpClient(client, configurations, logger);
    }
}
