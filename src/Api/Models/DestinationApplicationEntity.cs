/*
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
    /// Destination Application Entity
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "name": "MYPACS",
    ///     "hostIp": "10.20.100.200",
    ///     "aeTitle": "MONAIPACS",
    ///     "port": 1104
    /// }
    /// </code>
    /// </example>
    public class DestinationApplicationEntity : BaseApplicationEntity
    {
        /// <summary>
        /// Gets or sets the port to connect to.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///  Gets or sets the AE Title (AET) used to identify itself in a DICOM association.
        /// </summary>
        public string AeTitle { get; set; } = default!;

        public override void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = AeTitle;
        }

        public override string ToString()
        {
            return $"Name: {Name}/AET: {AeTitle}/Host: {HostIp}/Port: {Port}";
        }
    }
}
