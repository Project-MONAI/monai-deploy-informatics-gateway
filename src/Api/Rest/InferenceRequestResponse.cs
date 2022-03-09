// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Response message of a successful inference request.
    /// </summary>
    public class InferenceRequestResponse
    {
        /// <summary>
        /// Gets or sets the original request transaction ID.
        /// </summary>
        public string TransactionId { get; set; }
    }
}
