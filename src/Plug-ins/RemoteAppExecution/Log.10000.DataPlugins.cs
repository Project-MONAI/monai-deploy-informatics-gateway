/*
 * Copyright 2023 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 10000, Level = LogLevel.Debug, Message = "Changed {tag} from {originalValue} to {newValue}.")]
        public static partial void ValueChanged(this ILogger logger, string tag, string originalValue, string newValue);

        [LoggerMessage(EventId = 10001, Level = LogLevel.Error, Message = "Cannot find entry for incoming instance {sopInstanceUid}.")]
        public static partial void IncomingInstanceNotFound(this ILogger logger, string sopInstanceUid);
    }
}
