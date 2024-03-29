/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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

        /// <summary>
        /// Gets or sets whether to automatically validate the DICOM values when serializing to JSON.
        /// Defaults to False.
        /// </summary>
        public bool ValidateDicomOnSerialization { get; set; } = false;
    }
}
