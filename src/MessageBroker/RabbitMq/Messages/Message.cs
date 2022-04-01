// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Text;
using Newtonsoft.Json;

namespace Monai.Deploy.MessageBroker.Messages
{
    public sealed class Message : MessageBase
    {
        /// <summary>
        /// Body of the message.
        /// </summary>
        public byte[] Body { get; init; }

        public Message(byte[] body,
                       string messageDescription,
                       string messageId,
                       string applicationId,
                       string contentType,
                       string correlationId,
                       DateTime creationDateTime,
                       string deliveryTag = "")
            : base(messageId, messageDescription, contentType, applicationId, correlationId, creationDateTime)
        {
            Body = body;
            DeliveryTag = deliveryTag;
        }

        /// <summary>
        /// Converts <c>Body</c> from binary[] to JSON string and then the specified <c>T</c> type.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <returns>Instance of <c>T</c> or <c>null</c> if data cannot be deserialized.</returns>
        public T ConvertTo<T>()
        {
            var json = Encoding.UTF8.GetString(Body);
            return JsonConvert.DeserializeObject<T>(json)!;
        }
    }
}
