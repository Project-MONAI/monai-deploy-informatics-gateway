// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Logging;

namespace Monai.Deploy.MessageBroker.Common
{
    public static partial class Log
    {
        internal static readonly string LoggingScopeMessageApplication = "Message ID={0}. Application ID={1}.";

        [LoggerMessage(EventId = 10000, Level = LogLevel.Information, Message = "Publishing message to {endpoint}/{virtualHost}. Exchange={exchange}, Routing Key={topic}.")]
        public static partial void PublshingRabbitMq(this ILogger logger, string endpoint, string virtualHost, string exchange, string topic);

        [LoggerMessage(EventId = 10001, Level = LogLevel.Information, Message = "{ServiceName} connecting to {endpoint}/{virtualHost}.")]
        public static partial void ConnectingToRabbitMq(this ILogger logger, string serviceNAme, string endpoint, string virtualHost);

        [LoggerMessage(EventId = 10002, Level = LogLevel.Information, Message = "Message received from queue {queue} for {topic}.")]
        public static partial void MessageReceivedFromQueue(this ILogger logger, string queue, string topic);

        [LoggerMessage(EventId = 10003, Level = LogLevel.Information, Message = "Listening for messages from {endpoint}/{virtualHost}. Exchange={exchange}, Queue={queue}, Routing Key={topic}.")]
        public static partial void SubscribeToRabbitMqQueue(this ILogger logger, string endpoint, string virtualHost, string exchange, string queue, string topic);

        [LoggerMessage(EventId = 10004, Level = LogLevel.Information, Message = "Sending message acknowledgement for message {messageId}.")]
        public static partial void SendingAcknowledgement(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 10005, Level = LogLevel.Information, Message = "Ackowledge sent for message {messageId}.")]
        public static partial void AcknowledgementSent(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 10006, Level = LogLevel.Information, Message = "Sending nack message {messageId} and requeuing.")]
        public static partial void SendingNAcknowledgement(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 10007, Level = LogLevel.Information, Message = "Nack message sent for message {messageId}.")]
        public static partial void NAcknowledgementSent(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 10008, Level = LogLevel.Information, Message = "Closing connection.")]
        public static partial void ClosingConnection(this ILogger logger);
    }
}
