/*
 * Copyright 2011-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
