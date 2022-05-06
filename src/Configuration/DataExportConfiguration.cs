// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class DataExportConfiguration
    {
        /// <summary>
        /// Gets or sets retry configuration for data export agents.
        /// </summary>
        [ConfigurationKeyName("retries")]
        public RetryConfiguration Retries { get; set; } = new RetryConfiguration();
    }
}
