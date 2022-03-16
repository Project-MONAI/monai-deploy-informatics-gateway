// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Runtime.Serialization;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    [Serializable]
    public class ControlException : Exception
    {
        public int ErrorCode { get; }

        protected ControlException()
        {
        }

        protected ControlException(string message) : base(message)
        {
        }

        protected ControlException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ControlException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected ControlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = info.GetInt32(nameof(ErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Guard.Against.Null(info, nameof(info));

            info.AddValue(nameof(ErrorCode), ErrorCode);

            base.GetObjectData(info, context);
        }
    }
}
