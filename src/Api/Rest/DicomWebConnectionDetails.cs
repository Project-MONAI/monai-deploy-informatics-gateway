// SPDX-FileCopyrightText: © 2011-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
        public IList<InputInterfaceOperations> Operations { get; set; }

        /// <summary>
        /// Gets or sets the resource URI (Uniform Resource Identifier) of the connection.
        /// </summary>
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the authentication/authorization token of the connection.
        /// For HTTP basic access authentication, the value must be encoded in based 64 using "{username}:{password}" format.
        /// </summary>
        [JsonPropertyName("authID")]
        public string AuthId { get; set; }

        /// <summary>
        /// Gets or sets the type of the authentication token used for the connection.
        /// Defaults to None if not specified.
        /// </summary>
        [JsonPropertyName("authType")]
        public ConnectionAuthType AuthType { get; set; } = ConnectionAuthType.None;
    }
}
