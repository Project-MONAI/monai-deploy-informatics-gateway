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
