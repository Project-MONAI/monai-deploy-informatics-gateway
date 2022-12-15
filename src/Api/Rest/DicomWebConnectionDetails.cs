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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Connection details of a data source.
    /// </summary>
    public class DicomWebConnectionDetails
    {
        /// <summary>
        /// Gets or sets a list of permitted operations for the connection.
        /// </summary>
        [JsonPropertyName("operations")]
        public IList<InputInterfaceOperations>? Operations { get; set; }

        /// <summary>
        /// Gets or sets the resource URI (Uniform Resource Identifier) of the connection.
        /// </summary>
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets the authentication/authorization token of the connection.
        /// For HTTP basic access authentication, the value must be encoded in based 64 using "{username}:{password}" format.
        /// </summary>
        [JsonPropertyName("authID")]
        public string? AuthId { get; set; }

        /// <summary>
        /// Gets or sets the type of the authentication token used for the connection.
        /// Defaults to None if not specified.
        /// </summary>
        [JsonPropertyName("authType")]
        public ConnectionAuthType AuthType { get; set; } = ConnectionAuthType.None;
    }
}
