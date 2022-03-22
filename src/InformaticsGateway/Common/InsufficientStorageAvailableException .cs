// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Common
{
    [Serializable]
    public class InsufficientStorageAvailableException : Exception
    {
        public InsufficientStorageAvailableException()
        {
        }

        public InsufficientStorageAvailableException(string message) : base(message)
        {
        }

        public InsufficientStorageAvailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InsufficientStorageAvailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
