// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Client
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 50000, Level = LogLevel.Information, Message = "{message}")]
        public static partial void InfoMessage(this ILogger logger, string message);

        [LoggerMessage(EventId = 50001, Level = LogLevel.Warning, Message = "{message}")]
        public static partial void WarningMessage(this ILogger logger, string message);

        [LoggerMessage(EventId = 50100, Level = LogLevel.Debug, Message = "Base address set to {uri}")]
        public static partial void BaseAddressSet(this ILogger logger, Uri uri);

        [LoggerMessage(EventId = 50101, Level = LogLevel.Debug, Message = "Sending request to {route}")]
        public static partial void SendingRequestTo(this ILogger logger, string route);


    }
}
