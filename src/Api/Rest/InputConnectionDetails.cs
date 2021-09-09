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

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Connection details of a data source.
    /// </summary>
    public class InputConnectionDetails : DicomWebConnectionDetails
    {
        /// <summary>
        /// Gets or sets the name of the algorithm. Used when <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InputInterfaceType" />
        /// is <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InputInterfaceType.Algorithm" />.
        /// <c>Name</c> is also used as the job name.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the MONAI Application name or ID. Used when <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InputInterfaceType" />
        /// is <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InputInterfaceType.Algorithm" />.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
