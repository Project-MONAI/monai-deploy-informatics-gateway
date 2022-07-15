// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 800, Level = LogLevel.Information, Message = "New HL7 client connected.")]
        public static partial void ClientConnected(this ILogger logger);

        [LoggerMessage(EventId = 801, Level = LogLevel.Error, Message = "Error reading data, connection may be dropped.")]
        public static partial void ExceptionReadingClientStream(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 802, Level = LogLevel.Error, Message = "Error parsing HL7 message.")]
        public static partial void ErrorParsingHl7Message(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 803, Level = LogLevel.Warning, Message = "Unable to locate {segment} field {field} in the HL7 message.")]
        public static partial void MissingFieldInHL7Message(this ILogger logger, string segment, int field, Exception ex);

        [LoggerMessage(EventId = 804, Level = LogLevel.Error, Message = "Error sending HL7 acknowledgment.")]
        public static partial void ErrorSendingHl7Acknowledgment(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 805, Level = LogLevel.Information, Message = "Maximum number {maximumAllowedConcurrentConnections} of clients reached.")]
        public static partial void MaxedOutHl7Connections(this ILogger logger, int maximumAllowedConcurrentConnections);

        [LoggerMessage(EventId = 806, Level = LogLevel.Information, Message = "HL7 listening on port: {port}.")]
        public static partial void Hl7ListeningOnPort(this ILogger logger, int port);

        [LoggerMessage(EventId = 807, Level = LogLevel.Critical, Message = "Socket error: {error}")]
        public static partial void Hl7SocketException(this ILogger logger, string error);
    }
}
