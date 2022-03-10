// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Services.Common;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    public interface IMonaiServiceLocator
    {
        IEnumerable<IMonaiService> GetMonaiServices();

        Dictionary<string, ServiceStatus> GetServiceStatus();
    }
}
