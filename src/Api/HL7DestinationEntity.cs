﻿/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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

namespace Monai.Deploy.InformaticsGateway.Api.Models
{
    /// <summary>
    /// HL7 Destination Entity
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "name": "MYPACS",
    ///     "hostIp": "10.20.100.200",
    ///     "port": 1104
    /// }
    /// </code>
    /// </example>
    public class HL7DestinationEntity : BaseApplicationEntity
    {
        /// <summary>
        /// Gets or sets the port to connect to.
        /// </summary>
        public int Port { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}/Host: {HostIp}/Port: {Port}";
        }
    }
}
