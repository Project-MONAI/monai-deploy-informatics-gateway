// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Details of a DICOM instance to be retrieved for an inference request.
    /// </summary>
    /// <remarks>
    /// <para><c>SopInstanceUid></c> is required.</para>
    /// </remarks>
    public class RequestedInstance
    {
        /// <summary>
        /// Gets or sets the SOP Instance UID to be retrieved.
        /// </summary>
        [JsonProperty(PropertyName = "SOPInstanceUID")]
        public IList<string> SopInstanceUid { get; set; }
    }
}
