// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Details of a DICOM study to be retrieved for an inference request.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     ...
    ///     "studies" : [
    ///         "StudyInstanceUID": "1.2.3.4.555.6666.7777",
    ///         "series": [
    ///             "SeriesInstanceUID": "1.2.3.4.55.66.77.88",
    ///             "instances": [
    ///                 "SOPInstanceUID": [
    ///                     "1.2.3.4.5.6.7.8.99.1",
    ///                     "1.2.3.4.5.6.7.8.99.2",
    ///                     "1.2.3.4.5.6.7.8.99.3",
    ///                     ...
    ///                 ]
    ///             ]
    ///         ]
    ///     ]
    ///     ...
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para><c>StudyInstanceUid></c> is required.</para>
    /// <para>If <c>Series></c> is not specified, the entire study is retrieved.</para>
    /// </remarks>
    public class RequestedStudy
    {
        /// <summary>
        /// Gets or sets the Study Instance UID to be retrieved.
        /// </summary>
        [JsonProperty(PropertyName = "StudyInstanceUID")]
        public string StudyInstanceUid { get; set; }

        /// <summary>
        /// Gets or sets a list of DICOM series to be retrieved.
        /// </summary>
        [JsonProperty(PropertyName = "series")]
        public IList<RequestedSeries> Series { get; set; }
    }
}
