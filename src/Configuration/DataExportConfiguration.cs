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
    public class DataExportConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of retries the export service shall perform on an
        /// export task. Default 3 retries.
        /// </summary>
        [JsonProperty(PropertyName = "maximumRetries")]
        public int MaximumRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the poll frequency for the export task service.
        /// Defaults to 500ms.
        /// </summary>
        [JsonProperty(PropertyName = "pollFrequencyMs")]
        public int PollFrequencyMs { get; set; } = 500;
    }
}