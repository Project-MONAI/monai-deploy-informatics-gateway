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
        [LoggerMessage(EventId = 4000, Level = LogLevel.Warning, Message = "Failed to upload file {identifier}; added back to queue for retry.")]
        public static partial void FailedToUploadFile(this ILogger logger, string identifier, Exception ex);

        [LoggerMessage(EventId = 4001, Level = LogLevel.Debug, Message = "Upload statistics: {threads} threads, {seconds} seconds.")]
        public static partial void UploadStats(this ILogger logger, int threads, double seconds);

        [LoggerMessage(EventId = 4002, Level = LogLevel.Debug, Message = "Uploading file to temporary store at {filePath}.")]
        public static partial void UploadingFileToTemporaryStore(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Instance queued for upload {identifier}. Items in queue {count} using memory {memoryUsageKb}KB.")]
        public static partial void InstanceAddedToUploadQueue(this ILogger logger, string identifier, int count, double memoryUsageKb);

        [LoggerMessage(EventId = 4004, Level = LogLevel.Debug, Message = "Error removing objects that are pending upload during startup.")]
        public static partial void ErrorRemovingPendingUploadObjects(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 4005, Level = LogLevel.Debug, Message = "Error uploading temporary store. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorUploadingFileToTemporaryStore(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 4006, Level = LogLevel.Information, Message = "File uploaded to temporary store at {filePath}.")]
        public static partial void UploadedFileToTemporaryStore(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 4007, Level = LogLevel.Debug, Message = "Items in queue {count}.")]
        public static partial void InstanceInUploadQueue(this ILogger logger, int count);

        [LoggerMessage(EventId = 4008, Level = LogLevel.Error, Message = "Unknown error occurred while uploading.")]
        public static partial void ErrorUploading(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 4009, Level = LogLevel.Error, Message = "Failed to verify file existence {path}.")]
        public static partial void FailedToVerifyFileExistence(this ILogger logger, string path, Exception ex);

        [LoggerMessage(EventId = 4010, Level = LogLevel.Debug, Message = "File {path} exists={exists}.")]
        public static partial void VerifyFileExists(this ILogger logger, string path, bool exists);
    }
}
