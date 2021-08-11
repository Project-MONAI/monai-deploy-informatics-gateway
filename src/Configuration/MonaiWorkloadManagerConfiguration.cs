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

using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class MonaiWorkloadManagerConfiguration
    {
        public static int DefaultClientTimeout = 300;

        /// <summary>
        /// Gets or sets the URI of the Platform API.
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets maximum number of concurrent uploads to the Paylodas Service.
        /// </summary>
        [JsonProperty(PropertyName = "parallelUploads")]
        public int ParallelUploads { get; set; } = 4;

        /// <summary>
        /// Gets or sets the maximum number of retries to be performed when an execution attempt fails to connect to MONAI Workload Manager.
        /// </summary>
        [JsonProperty(PropertyName = "maxRetries")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets number of seconds to wait before attempting to retry.
        /// </summary>
        [JsonProperty(PropertyName = "retryDelaySeconds")]
        public int RetryDelaySeconds { get; set; } = 180;

        /// <summary>
        /// Gets or sets the client connection timeout in seconds.
        /// </summary>
        [JsonProperty(PropertyName = "clientTimeout")]
        public int ClientTimeoutSeconds { get; set; } = DefaultClientTimeout;

        public MonaiWorkloadManagerConfiguration()
        {
        }
    }
}