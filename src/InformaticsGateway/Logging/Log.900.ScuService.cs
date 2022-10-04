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

using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 900, Level = LogLevel.Information, Message = "Association accepted.")]
        public static partial void ScuAssociationAccepted(this ILogger logger);

        [LoggerMessage(EventId = 901, Level = LogLevel.Warning, Message = "Association rejected.")]
        public static partial void ScuAssociationRejected(this ILogger logger);

        [LoggerMessage(EventId = 902, Level = LogLevel.Information, Message = "Association released.")]
        public static partial void ScuAssociationReleased(this ILogger logger);

        [LoggerMessage(EventId = 903, Level = LogLevel.Information, Message = "C-Echo completed successfully.")]
        public static partial void CEchoSuccess(this ILogger logger);

        [LoggerMessage(EventId = 904, Level = LogLevel.Error, Message = "C-Echo failed with {status}.")]
        public static partial void CEchoFailure(this ILogger logger, string status);
    }
}
