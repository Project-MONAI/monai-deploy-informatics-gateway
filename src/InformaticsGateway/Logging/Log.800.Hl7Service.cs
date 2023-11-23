/*
 * Copyright 2022 MONAI Consortium
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

        [LoggerMessage(EventId = 808, Level = LogLevel.Critical, Message = "Error handling HL7 results.")]
        public static partial void ErrorHandlingHl7Results(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 809, Level = LogLevel.Debug, Message = "Acknowledgment type={value}.")]
        public static partial void AcknowledgmentType(this ILogger logger, string value);

        [LoggerMessage(EventId = 810, Level = LogLevel.Information, Message = "Acknowledgment sent: length={length}.")]
        public static partial void AcknowledgmentSent(this ILogger logger, int length);

        [LoggerMessage(EventId = 811, Level = LogLevel.Debug, Message = "HL7  bytes received: {length}.")]
        public static partial void Hl7MessageBytesRead(this ILogger logger, int length);

        [LoggerMessage(EventId = 812, Level = LogLevel.Debug, Message = "Parsing message with {length} bytes.")]
        public static partial void Hl7GenerateMessage(this ILogger logger, int length);

        [LoggerMessage(EventId = 813, Level = LogLevel.Debug, Message = "Waiting for HL7 message.")]
        public static partial void HL7ReadingMessage(this ILogger logger);

        [LoggerMessage(EventId = 814, Level = LogLevel.Warning, Message = "HL7 service paused due to insufficient storage space.  Available storage space: {availableFreeSpace:D}.")]
        public static partial void Hl7DisconnectedDueToLowStorageSpace(this ILogger logger, long availableFreeSpace);

        [LoggerMessage(EventId = 815, Level = LogLevel.Information, Message = "HL7 client {clientId} disconnected.")]
        public static partial void Hl7ClientRemoved(this ILogger logger, Guid clientId);

        [LoggerMessage(EventId = 816, Level = LogLevel.Debug, Message = "HL7 config loaded. {config}")]
        public static partial void Hl7ConfigLoaded(this ILogger logger, string config);

        [LoggerMessage(EventId = 817, Level = LogLevel.Information, Message = "No HL7 config found")]
        public static partial void Hl7NoConfig(this ILogger logger);

        [LoggerMessage(EventId = 818, Level = LogLevel.Debug, Message = "HL7 no matching config found for message {message}")]
        public static partial void Hl7NoMatchingConfig(this ILogger logger, string message);

        [LoggerMessage(EventId = 819, Level = LogLevel.Debug, Message = "HL7 found matching config found for. {Id} config: {config}")]
        public static partial void Hl7FoundMatchingConfig(this ILogger logger, string Id, string config);

        [LoggerMessage(EventId = 820, Level = LogLevel.Warning, Message = "HL7 exception thrown extracting Hl7 Info")]
        public static partial void Hl7ExceptionThrow(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 821, Level = LogLevel.Warning, Message = "HL7 external App Details not found")]
        public static partial void Hl7ExtAppDetailsNotFound(this ILogger logger);

        [LoggerMessage(EventId = 822, Level = LogLevel.Debug, Message = "HL7 changing value {hl7Tag} from {oldValue} to {newValue}")]
        public static partial void ChangingHl7Values(this ILogger logger, string hl7Tag, string oldValue, string newValue);

    }
}
