// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    /// <summary>
    /// Represents the <c>dicom</c> section of the configuration file.
    /// </summary>
    public class DicomConfiguration
    {
        /// <summary>
        /// Represents the <c>dicom>scp</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("scp")]
        public ScpConfiguration Scp { get; set; } = new ScpConfiguration();

        /// <summary>
        /// Represents the <c>dicom>scu</c> section of the configuration file.
        /// </summary>
        [ConfigurationKeyName("scu")]
        public ScuConfiguration Scu { get; set; } = new ScuConfiguration();

        /// <summary>
        /// Gets or sets whether to write DICOM JSON file for each instance received.
        /// </summary>
        [ConfigurationKeyName("writeDicomJson")]
        public DicomJsonOptions WriteDicomJson { get; set; } = DicomJsonOptions.IgnoreOthers;
    }
}
