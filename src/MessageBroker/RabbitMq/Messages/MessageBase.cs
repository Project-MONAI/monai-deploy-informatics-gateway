// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Ardalis.GuardClauses;

namespace Monai.Deploy.MessageBroker.Messages
{
    public abstract class MessageBase
    {
        /// <summary>
        /// UUID for the message formatted with hyphens.
        /// xxxxxxxx-xxxx-Mxxx-Nxxx-xxxxxxxxxxxx
        /// </summary>
        public string MessageId { get; init; }

        /// <summary>
        /// A short description of the type serialized in the message body.
        /// </summary>
        public string MessageDescription { get; init; }

        /// <summary>
        /// Content or MIME type of the message body.
        /// </summary>
        public string ContentType { get; init; }

        /// <summary>
        /// UUID of the application, in this case, the Informatics Gateway.
        /// The UUID of Informatics Gateway is <code>16988a78-87b5-4168-a5c3-2cfc2bab8e54</code>.
        /// </summary>
        public string ApplicationId { get; init; }

        /// <summary>
        /// Correlation ID of the message.
        /// For DIMSE connections, the ID generated during association is used.
        /// For ACR inference requests, the Transaction ID provided in the request is used.
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>
        /// Datetime the message is created.
        /// </summary>
        public DateTime CreationDateTime { get; init; }

        /// <summary>
        /// Gets or set the delivery tag/acknoweldge token for the message.
        /// </summary>
        public string DeliveryTag { get; init; }

        protected MessageBase(string messageId,
                              string messageDescription,
                              string contentType,
                              string applicationId,
                              string correlationId,
                              DateTime creationDateTime)
        {
            Guard.Against.NullOrWhiteSpace(messageId, nameof(messageId));
            Guard.Against.NullOrWhiteSpace(messageDescription, nameof(messageDescription));
            Guard.Against.NullOrWhiteSpace(contentType, nameof(contentType));
            Guard.Against.NullOrWhiteSpace(applicationId, nameof(applicationId));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));

            MessageId = messageId;
            MessageDescription = messageDescription;
            ContentType = contentType;
            ApplicationId = applicationId;
            CorrelationId = correlationId;
            CreationDateTime = creationDateTime;
            DeliveryTag = string.Empty;
        }
    }
}
