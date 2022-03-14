// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Text;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public sealed class JsonMessage<T> : MessageBase
    {
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
            ContentType = MessageContentTypes.JsonApplicationType;
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
