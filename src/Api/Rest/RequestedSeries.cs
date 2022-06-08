// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Details of a DICOM series to be retrieved for an inference request.
    /// </summary>
    /// <remarks>
    /// <para><c>SeriesInstanceUID></c> is required.</para>
    /// <para>If <c>instances></c> is not specified, the entire series is retrieved.</para>
    /// </remarks>
    public class RequestedSeries
    {
        /// <summary>
        /// Gets or sets the Series Instance UID to be retrieved.
        /// </summary>
        [JsonPropertyName("SeriesInstanceUID")]
        public string SeriesInstanceUid { get; set; }

        /// <summary>
        /// Gets or sets a list of DICOM instances to be retrieved.
        /// </summary>
        [JsonPropertyName("instances")]
        public IList<RequestedInstance> Instances { get; set; }
    }
}
