// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class FhirConfiguration
    {
        public static readonly int DefaultClientTimeout = 300;

        /// <summary>
        /// Gets or sets the client connection timeout in seconds.
        /// </summary>
        [ConfigurationKeyName("clientTimeout")]
        public int ClientTimeoutSeconds { get; set; } = DefaultClientTimeout;

        public FhirConfiguration()
        {
        }
    }
}
