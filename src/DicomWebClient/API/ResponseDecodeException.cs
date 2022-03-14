// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    [Serializable]
    public class ResponseDecodeException : Exception
    {
        public ResponseDecodeException(string message) : base(message)
        {
        }

        protected ResponseDecodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
