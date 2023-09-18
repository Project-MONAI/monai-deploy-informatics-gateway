/*
 * Copyright 2023 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public class PostPayloadException : Exception
    {
        public Payload.PayloadState TargetQueue { get; }
        public Payload? Payload { get; }

        public PostPayloadException()
        {
        }

        public PostPayloadException(Api.Storage.Payload.PayloadState targetState, Payload payload)
        {
            TargetQueue = targetState;
            Payload = payload;
        }

        public PostPayloadException(string message) : base(message)
        {
        }

        public PostPayloadException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PostPayloadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
