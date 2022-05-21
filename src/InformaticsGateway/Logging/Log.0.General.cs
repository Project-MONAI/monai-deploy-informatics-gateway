// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{ServiceName} started.")]
        public static partial void ServiceStarted(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{ServiceName} is stopping.")]
        public static partial void ServiceStopping(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "{ServiceName} canceled.")]
        public static partial void ServiceCancelled(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "{ServiceName} canceled.")]
        public static partial void ServiceCancelledWithException(this ILogger logger, string serviceName, Exception ex);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "{ServiceName} may be disposed.")]
        public static partial void ServiceDisposed(this ILogger logger, string serviceName, Exception ex);

        [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "{ServiceName} is running.")]
        public static partial void ServiceRunning(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Waiting for {ServiceName} to stop.")]
        public static partial void ServiceStopPending(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Error querying database.")]
        public static partial void ErrorQueryingDatabase(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 9, Level = LogLevel.Critical, Message = "Type '{type}' cannot be found.")]
        public static partial void TypeNotFound(this ILogger logger, string type);

        [LoggerMessage(EventId = 10, Level = LogLevel.Critical, Message = "Instance of '{type}' cannot be found.")]
        public static partial void InstanceOfTypeNotFound(this ILogger logger, string type);

        [LoggerMessage(EventId = 11, Level = LogLevel.Critical, Message = "Instance of '{type}' cannot be found.")]
        public static partial void ServiceInvalidOrCancelled(this ILogger logger, string type, Exception ex);

        [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "{ServiceName} starting.")]
        public static partial void ServiceStarting(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 13, Level = LogLevel.Critical, Message = "Failed to start {ServiceName}.")]
        public static partial void ServiceFailedToStart(this ILogger logger, string serviceName, Exception ex);
    }
}
