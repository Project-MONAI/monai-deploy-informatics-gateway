// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Api
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
    }
}
