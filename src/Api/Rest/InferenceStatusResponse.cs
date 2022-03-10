// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Response message of a inference status query.
    /// </summary>
    public class InferenceStatusResponse
    {
        /// <summary>
        /// Gets or set the transaction ID of a request.
        /// </summary>
        public string TransactionId { get; set; }
    }
}
