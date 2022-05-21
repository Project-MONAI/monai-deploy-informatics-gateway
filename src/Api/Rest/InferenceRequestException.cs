// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Inference request exception.
    /// </summary>
    [Serializable]
    public class InferenceRequestException : Exception
    {
        public InferenceRequestException(string message) : base(message)
        {
        }

        protected InferenceRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
