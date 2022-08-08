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
        [LoggerMessage(EventId = 2100, Level = LogLevel.Error, Message = "Error saving file storage object. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorSavingFileStorageMetadata(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 2101, Level = LogLevel.Debug, Message = "Storage metadata saved to the database.")]
        public static partial void StorageMetadataSaved(this ILogger logger);

        [LoggerMessage(EventId = 2102, Level = LogLevel.Error, Message = "Error deleting objects that are pending upload. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorDeletingPendingUploads(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);
    }
}
