// SPDX-FileCopyrightText: © 2011-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Response message of a successful inference request.
    /// </summary>
    public class HealthStatusResponse
    {
        /// <summary>
        /// Gets or sets the number of active DIMSE connetions.
        /// </summary>
        public int ActiveDimseConnections { get; set; }

        /// <summary>
        /// Gets or sets status of the MONAI Deploy Informatics Gateway services.
        /// </summary>
        public Dictionary<string, ServiceStatus> Services { get; set; } = new Dictionary<string, ServiceStatus>(StringComparer.OrdinalIgnoreCase);
    }
}
