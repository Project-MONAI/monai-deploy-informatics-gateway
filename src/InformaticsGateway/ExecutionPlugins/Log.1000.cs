/*
 * Copyright 2021-2023 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.ExecutionPlugins
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Changed the StudyUid from {OriginalStudyUid} to {NewStudyUid}")]
        public static partial void LogStudyUidChanged(this ILogger logger, string OriginalStudyUid, string NewStudyUid);

        [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Cannot find entry for OriginalStudyUid {OriginalStudyUid} ")]
        public static partial void LogOriginalStudyUidNotFound(this ILogger logger, string OriginalStudyUid);
    }
}
