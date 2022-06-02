// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Text.Json.Serialization;

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
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the MONAI Application name or ID. Used when <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InputInterfaceType" />
        /// is <see cref="T:Monai.Deploy.InformaticsGateway.Api.Rest.InputInterfaceType.Algorithm" />.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
