// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System;
using System.Text;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public sealed class Message : MessageBase
    {
        /// <summary>
        /// Body of the message.
        /// </summary>
        public byte[] Body { get; init; }

        public Message(byte[] body,
                       string bodyDescription,
                       string contentType,
                       string correlationId,
                       string deliveryTag)
            : this(body,
                   bodyDescription,
                   Guid.NewGuid().ToString(),
                   Message.InformaticsGatewayApplicationId,
                   contentType,
                   correlationId,
                   DateTime.UtcNow,
                   deliveryTag)
        {
        }

        public Message(byte[] body,
                       string bodyDescription,
                       string messageId,
                       string applicationId,
                       string contentType,
                       string correlationId,
                       DateTime creationDateTime,
                       string deliveryTag)
        {
            Body = body;
            MessageDescription = bodyDescription;
            MessageId = messageId;
            ApplicationId = applicationId;
            ContentType = contentType;
            CorrelationId = correlationId;
            CreationDateTime = creationDateTime;
            DeliveryTag = deliveryTag;
        }

        /// <summary>
        /// Converts <c>Body</c> from binary[] to JSON string and then the specified <c>T</c> type.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <returns></returns>
        public T ConvertTo<T>()
        {
            var json = Encoding.UTF8.GetString(Body);
            var data = JsonConvert.DeserializeObject<T>(json);
            return data;
        }
    }
}
