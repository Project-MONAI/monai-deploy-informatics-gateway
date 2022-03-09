// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Inference request exception.
    /// </summary>
    public class InferenceRequestException : Exception
    {
        public InferenceRequestException(string message) : base(message)
        {
        }
    }
}
