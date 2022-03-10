// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class DicomWebConfiguration
    {
        public static readonly int DefaultClientTimeout = 3600;

        /// <summary>
        /// Gets or sets the client connection timeout in seconds.
        /// </summary>
        [JsonProperty(PropertyName = "clientTimeout")]
        public int ClientTimeoutSeconds { get; set; } = DefaultClientTimeout;

        /// <summary>
        /// Gets or sets the (postfix) name of the DICOMweb export agent used for receiving messages.
        /// The agent name is combine with <see cref="MessageBrokerConfigurationKeys.ExportRequestPrefix"/>
        /// for subscribing messages from the message broker service.
        [JsonProperty(PropertyName = "agentName")]
        public string AgentName { get; set; } = "monaidicomweb";

        /// <summary>
        /// Gets or sets the maximum number of simultaneous DICOMweb connections.
        /// </summary>
        [JsonProperty(PropertyName = "maximumNumberOfConnections")]
        public int MaximumNumberOfConnection { get; set; } = 2;

        public DicomWebConfiguration()
        {
        }
    }
}
