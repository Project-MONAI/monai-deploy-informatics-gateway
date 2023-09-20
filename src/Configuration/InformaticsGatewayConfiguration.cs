/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    /// <summary>
    /// Main class used when deserializing the application configuration file.
    /// </summary>
    public class InformaticsGatewayConfiguration
    {
        /// <summary>
        /// Represents the <c>dicom</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("dicom")]
        public DicomConfiguration Dicom { get; set; }

        /// <summary>
        /// Represents the <c>storage</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("storage")]
        public StorageConfiguration Storage { get; set; }

        /// <summary>
        /// Represents the <c>dicomWeb</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("dicomWeb")]
        public DicomWebConfiguration DicomWeb { get; set; }

        /// <summary>
        /// Represents the <c>fhir</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("fhir")]
        public FhirConfiguration Fhir { get; set; }

        /// <summary>
        /// Represents the <c>hl7</c> section of the configuration file.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("hl7")]
        public Hl7Configuration Hl7 { get; set; }

        /// <summary>
        /// Represents the <c>export</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("export")]
        public DataExportConfiguration Export { get; set; }

        /// <summary>
        /// Represents the <c>messaging</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("messaging")]
        public MessageBrokerConfiguration Messaging { get; set; }

        /// <summary>
        /// Represents the <c>database</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("database")]
        public DatabaseConfiguration Database { get; set; }

        /// <summary>
        /// Represents the <c>pluginConfiguration</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("plugins")]
        public PlugInConfiguration PlugInConfigurations { get; set; }

        /// <summary>
        /// Represents the <c>endpointSettings</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("endpointSettings")]
        public EndpointSettings EndpointSettings { get; set; }

        public InformaticsGatewayConfiguration()
        {
            Dicom = new DicomConfiguration();
            Storage = new StorageConfiguration();
            DicomWeb = new DicomWebConfiguration();
            Fhir = new FhirConfiguration();
            Export = new DataExportConfiguration();
            Messaging = new MessageBrokerConfiguration();
            Database = new DatabaseConfiguration();
            Hl7 = new Hl7Configuration();
            PlugInConfigurations = new PlugInConfiguration();
            EndpointSettings = new EndpointSettings();
        }
    }
}
