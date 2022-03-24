// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Newtonsoft.Json;

namespace Monai.Deploy.MessageBroker.Common
{
    public class BlockStorageInfo
    {
        /// <summary>
        /// Gets or sets the root path to the file.
        /// </summary>
        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; } = default!;

        /// <summary>
        /// Gets or sets the root path to the metadata file.
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public string Metadata { get; set; } = default!;
    }
}
