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
    /// Main class used when deserializing the application configuration file.
    /// </summary>
    public class InformaticsGatewayConfiguration
    {
        /// <summary>
        /// Name of the key for retrieve database connection string.
        /// </summary>
        public const string DatabaseConnectionStringKey = "InformaticsGatewayDatabase";

        /// <summary>
        /// Represents the <c>dicom</c> section of the configuration file.
        /// </summary>
        [JsonProperty(PropertyName = "dicom")]
        public DicomConfiguration Dicom { get; set; }

        /// <summary>
        /// Represents the <c>storage</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "storage")]
        public StorageConfiguration Storage { get; set; }

        /// <summary>
        /// Represents the <c>dicomWeb</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "dicomWeb")]
        public DicomWebConfiguration DicomWeb { get; set; }

        /// <summary>
        /// Represents the <c>fhir</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "fhir")]
        public FhirConfiguration Fhir { get; set; }

        /// <summary>
        /// Represents the <c>export</c> section of the configuration file.
        /// </summary>
        [JsonProperty(PropertyName = "export")]
        public DataExportConfiguration Export { get; set; }

        /// <summary>
        /// Represents the <c>messaging</c> section of the configuration file.
        /// </summary>
        [JsonProperty(PropertyName = "messaging")]
        public MessageBrokerConfiguration Messaging { get; set; }

        public InformaticsGatewayConfiguration()
        {
            Dicom = new DicomConfiguration();
            Storage = new StorageConfiguration();
            DicomWeb = new DicomWebConfiguration();
            Fhir = new FhirConfiguration();
            Export = new DataExportConfiguration();
            Messaging = new MessageBrokerConfiguration();
        }
    }
}
