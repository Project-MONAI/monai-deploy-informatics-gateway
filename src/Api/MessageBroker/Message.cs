// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Text;
using Newtonsoft.Json;

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
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
