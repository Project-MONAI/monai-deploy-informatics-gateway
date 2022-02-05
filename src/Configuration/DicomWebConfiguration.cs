// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class DicomWebConfiguration
    {
        public static int DefaultClientTimeout = 3600;

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
