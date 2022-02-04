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

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    /// <summary>
    /// Represents <c>dicom>scu</c> section of the configuration file.
    /// </summary>
    public class ScuConfiguration
    {
        /// <summary>
        /// Gets or sets the AE Title for SCU service.
        /// </summary>
        [JsonProperty(PropertyName = "aeTitle")]
        public string AeTitle { get; set; } = "MONAISCU";

        /// <summary>
        /// Gets or sets the (postfix) name of the DIMSE export agent used for receiving messages.
        /// The agent name is combine with <see cref="MessageBrokerConfigurationKeys.ExportRequestPrefix"/>
        /// for subscribing messages from the message broker service.
        /// </summary>
        [JsonProperty(PropertyName = "agentName")]
        public string AgentName { get; set; } = "monaiscu";

        /// <summary>
        /// Gets or sets whether or not to write message to log for each P-Data-TF PDU sent or received.
        /// </summary>
        [JsonProperty(PropertyName = "logDataPDUs")]
        public bool LogDataPdus { get; set; } = false;

        /// <summary>
        /// Gets or sets whether or not to write command and data datasets to the log.
        /// </summary>
        [JsonProperty(PropertyName = "logDimseDatasets")]
        public bool LogDimseDatasets { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of simultaneous DICOM associations for the SCU service.
        /// </summary>
        [JsonProperty(PropertyName = "maximumNumberOfAssociations")]
        public int MaximumNumberOfAssociations { get; set; } = 8;

        public ScuConfiguration()
        {
        }
    }
}
