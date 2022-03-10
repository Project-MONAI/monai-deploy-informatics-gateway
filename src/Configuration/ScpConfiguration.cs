// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Newtonsoft.Json;

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
