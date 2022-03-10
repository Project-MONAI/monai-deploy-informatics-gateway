// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public class BlockStorageInfo
    {
        /// <summary>
        /// Gets or sets the name of bucket where the file is stored.
        /// </summary>
        [JsonProperty(PropertyName = "bucket")]
        public string Bucket { get; set; }

        /// <summary>
        /// Gets or sets the root path to the file.
        /// </summary>
        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the root path to the metadata file.
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public string Metadata { get; set; }
    }
}
