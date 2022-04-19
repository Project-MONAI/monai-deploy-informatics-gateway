// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

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
        /// Represents the <c>export</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("export")]
        public DataExportConfiguration Export { get; set; }

        /// <summary>
        /// Represents the <c>messaging</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("messaging")]
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
