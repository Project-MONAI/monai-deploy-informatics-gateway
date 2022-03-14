// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    [Serializable]
    public class UnsupportedReturnTypeException : Exception
    {
        public UnsupportedReturnTypeException(string message) : base(message)
        {
        }

        protected UnsupportedReturnTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
