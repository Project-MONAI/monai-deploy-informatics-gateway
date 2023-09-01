/*
 * Copyright 2021-2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
