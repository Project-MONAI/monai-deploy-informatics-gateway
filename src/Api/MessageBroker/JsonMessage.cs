// Copyright 2021-2022 MONAI Consortium
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
    public sealed class JsonMessage<T> : MessageBase
    {
        public static readonly string JsonApplicationType = "application/json";

        /// <summary>
        /// Body of the message.
        /// </summary>
        public T Body { get; init; }

        public JsonMessage(T body,
                       string correlationId,
                       string deliveryTag)
            : this(body,
                   body.GetType().Name,
                   Guid.NewGuid().ToString(),
                   Message.InformaticsGatewayApplicationId,
                   correlationId,
                   DateTime.UtcNow,
                   deliveryTag)
        {
        }

        public JsonMessage(T body,
                       string bodyDescription,
                       string messageId,
                       string applicationId,
                       string correlationId,
                       DateTime creationDateTime,
                       string deliveryTag)
        {
            if (body is null)
            {
                throw new ArgumentException($"'{nameof(body)}' cannot be null or empty.", nameof(body));
            }
            if (string.IsNullOrEmpty(bodyDescription))
            {
                throw new ArgumentException($"'{nameof(bodyDescription)}' cannot be null or empty.", nameof(bodyDescription));
            }

            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException($"'{nameof(messageId)}' cannot be null or empty.", nameof(messageId));
            }

            if (string.IsNullOrWhiteSpace(applicationId))
            {
                throw new ArgumentException($"'{nameof(applicationId)}' cannot be null or empty.", nameof(applicationId));
            }

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                throw new ArgumentException($"'{nameof(correlationId)}' cannot be null or empty.", nameof(correlationId));
            }

            Body = body;
            MessageDescription = bodyDescription;
            MessageId = messageId;
            ApplicationId = applicationId;
            ContentType = JsonApplicationType;
            CorrelationId = correlationId;
            CreationDateTime = creationDateTime;
            DeliveryTag = deliveryTag;
        }

        /// <summary>
        /// Converts <c>Body</c> to JSON and then binary[].
        /// </summary>
        /// <returns></returns>
        public Message ToMessage()
        {
            var json = JsonConvert.SerializeObject(Body);

            return new Message(
                Encoding.UTF8.GetBytes(json),
                Body.GetType().Name,
                MessageId,
                ApplicationId,
                ContentType,
                CorrelationId,
                CreationDateTime,
                DeliveryTag);
        }
    }
}