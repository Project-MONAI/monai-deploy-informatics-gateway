// © 2021-2022, MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    [Serializable]
    public class ControlException : Exception
    {
        public int ErrorCode { get; }

        private ControlException()
        {
        }

        public ControlException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected ControlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
