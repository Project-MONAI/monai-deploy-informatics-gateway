// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    [Serializable]
    internal class ConvertStreamException : Exception
    {
        public ConvertStreamException()
        {
        }

        public ConvertStreamException(string message) : base(message)
        {
        }

        public ConvertStreamException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConvertStreamException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
