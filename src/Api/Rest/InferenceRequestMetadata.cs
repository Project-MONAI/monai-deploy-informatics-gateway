// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Represents the metadata associated with an inference request.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     ...
    ///     "inputMetadata" : {
    ///         "details" : { ... },
    ///         "inputs": [ ... ]
    ///     }
    ///     ...
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para><c>details></c> is required.</para>
    /// </remarks>
    public class InferenceRequestMetadata
    {
        /// <summary>
        /// Gets or sets the details of an inference request.
        /// </summary>
        [JsonProperty(PropertyName = "details")]
        public InferenceRequestDetails Details { get; set; }

        /// <summary>
        /// Gets or sets an array of inference request details.
        /// Note: this is an extension to the ACR specs to enable multiple input data types.
        /// </summary>
        [JsonProperty(PropertyName = "inputs")]
        public IList<InferenceRequestDetails> Inputs { get; set; }
    }
}
