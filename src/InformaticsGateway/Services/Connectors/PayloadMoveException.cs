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

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    public class PayloadNotifyException : Exception
    {
        public FailureReason Reason { get; }
        public bool ShallRetry { get; }

        public enum FailureReason
        {
            Unknown,
            IncorrectState,
            IncompletePayload,
            MoveFailure,
            ServiceUnavailable,
        }

        public PayloadNotifyException(FailureReason reason) : this(reason, true)
        {
        }

        public PayloadNotifyException(FailureReason reason, bool shllRetry)
        {
            Reason = reason;
            ShallRetry = shllRetry;
        }

        protected PayloadNotifyException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }

        public PayloadNotifyException()
        {
        }

        public PayloadNotifyException(string message) : base(message)
        {
        }

        public PayloadNotifyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
