// Copyright 2021-2022, MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ControlException : Exception
    {
        public int ErrorCode { get; }

        public ControlException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
