// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class DicomWebConfiguration
    {
        public static readonly int DefaultClientTimeout = 3600;

        /// <summary>
        /// Gets or sets the client connection timeout in seconds.
        /// </summary>
        [ConfigurationKeyName("clientTimeout")]
        public int ClientTimeoutSeconds { get; set; } = DefaultClientTimeout;

        /// <summary>
        /// Gets or sets the (postfix) name of the DICOMweb export agent used for receiving messages.
        /// The agent name is combine with <see cref="MessageBrokerConfigurationKeys.ExportRequestPrefix"/>
        /// for subscribing messages from the message broker service.
        [ConfigurationKeyName("agentName")]
        public string AgentName { get; set; } = "monaidicomweb";

        /// <summary>
        /// Gets or sets the maximum number of simultaneous DICOMweb connections.
        /// </summary>
        [ConfigurationKeyName("maximumNumberOfConnections")]
        public int MaximumNumberOfConnection { get; set; } = 2;

        /// <summary>
        /// Gets or set the maximum allowed file size in bytes with default to 2GiB.
        /// </summary>
        [ConfigurationKeyName("maxAllowedFileSize")]
        public long MaxAllowedFileSize { get; set; } = 2147483648;

        /// <summary>
        /// Gets or set the maximum memory buffer size in bytes with default to 30MiB.
        /// </summary>
        [ConfigurationKeyName("memoryThreshold")]
        public int MemoryThreshold { get; set; } = 31457280;

        /// <summary>
        /// Timeout, in seconds, to wait for instances before notifying other subsystems of data arrival
        /// for the specified data group.
        /// Defaults to two seconds.
        /// Note: the currently implementation of DICOMweb expects the entire payload to be received in a
        /// single POST request, therefore, the timeout value may be insignificant unless the load of the
        /// network affects the upload speed.
        /// </summary>
        public uint Timeout { get; set; } = 2;

        public DicomWebConfiguration()
        {
        }
    }
}
