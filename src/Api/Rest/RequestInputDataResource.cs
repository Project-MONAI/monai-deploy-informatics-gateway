// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Represents an input resource (data source).
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     ...
    ///     "inputResources" : [
    ///         {
    ///             "interface": "Algorithm",
    ///             "connectionDetails" : {
    ///                 "name": "ai-lung-tumor",
    ///                 "id": "123456790"
    ///             }
    ///         },
    ///         {
    ///             "interface": "DICOMweb",
    ///             "connectionDetails" : {
    ///                 "operations": [ "QUERY", "RETRIEVE" ],
    ///                 "uri": "http://host:port/dicomweb/",
    ///                 "authID": "dXNlcm5hbWU6cGFzc3dvcmQ=",
    ///                 "authType": "Basic"
    ///             }
    ///         }
    ///     ]
    ///     ...
    /// }
    /// </code>
    /// </example>
    public class RequestInputDataResource
    {
        /// <summary>
        /// Gets or sets the type of interface or a data source.
        /// </summary>
        [JsonPropertyName("interface")]
        public InputInterfaceType Interface { get; set; }

        /// <summary>
        /// Gets or sets connection details of a data source.
        /// </summary>
        [JsonPropertyName("connectionDetails")]
        public InputConnectionDetails ConnectionDetails { get; set; }
    }
}
