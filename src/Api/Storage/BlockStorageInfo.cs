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
