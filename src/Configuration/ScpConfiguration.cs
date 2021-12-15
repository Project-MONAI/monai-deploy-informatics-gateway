// Copyright 2021 MONAI Consortium
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
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    /// <summary>
    /// Represents <c>dicom>scp</c> section of the configuration file.
    /// </summary>
    public class ScpConfiguration
    {
        public const bool DefaultLogDimseDatasets = false;
        public const int DefaultMaximumNumberOfAssociations = 25;

        /// <summary>
        /// Gets or sets Port number to be used for SCP service.
        /// </summary>
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; } = 104;

        /// <summary>
        /// Gets or sets maximum number of simultaneous DICOM associations for the SCP service.
        /// </summary>
        [JsonProperty(PropertyName = "maximumNumberOfAssociations")]
        public int MaximumNumberOfAssociations { get; set; } = DefaultMaximumNumberOfAssociations;

        /// <summary>
        /// Gets or sets wheather or not to enable verification (C-ECHO) service
        /// </summary>
        [JsonProperty(PropertyName = "verification")]
        public bool EnableVerification { get; set; } = true;

        /// <summary>
        /// Gets or sets whether or not associations shall be rejected if not defined in the <c>dicom>scp>sources</c> section.
        /// </summary>
        [JsonProperty(PropertyName = "rejectUnknownSources")]
        public bool RejectUnknownSources { get; set; } = true;

        /// <summary>
        /// Gets or sets whether or not to write command and data datasets to the log.
        /// </summary>
        [JsonProperty(PropertyName = "logDimseDatasets")]
        public bool LogDimseDatasets { get; set; } = DefaultLogDimseDatasets;

        public IList<string> VerificationServiceTransferSyntaxes = new List<string> {
                    "1.2.840.10008.1.2.1", //Explicit VR Little Endian
                    "1.2.840.10008.1.2" , //Implicit VR Little Endian
                    "1.2.840.10008.1.2.2", //Explicit VR Big Endian
                };

        public ScpConfiguration()
        {
        }
    }
}
