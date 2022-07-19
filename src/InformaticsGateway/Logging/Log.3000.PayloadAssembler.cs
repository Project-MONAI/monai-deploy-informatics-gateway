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
        [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Restoring payloads from database.")]
        public static partial void RestorePayloads(this ILogger logger);

        [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Payload {payloadId} restored from database.")]
        public static partial void PayloadRestored(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "{count} payloads restored from database.")]
        public static partial void TotalNumberOfPayloadsRestored(this ILogger logger, int count);

        [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "File added to bucket {key}. Queue size: {count}")]
        public static partial void FileAddedToBucket(this ILogger logger, string key, int count);

        [LoggerMessage(EventId = 3004, Level = LogLevel.Trace, Message = "Number of buckets active: {count}.")]
        public static partial void BucketActive(this ILogger logger, int count);

        [LoggerMessage(EventId = 3005, Level = LogLevel.Trace, Message = "Checking elapsed time for bucket: {key}.")]
        public static partial void BucketElapsedTime(this ILogger logger, string key);

        [LoggerMessage(EventId = 3006, Level = LogLevel.Warning, Message = "Dropping Bucket {key} due to empty.")]
        public static partial void DropEmptyBucket(this ILogger logger, string key);

        [LoggerMessage(EventId = 3007, Level = LogLevel.Information, Message = "Bucket {key} sent to processing queue with {count} files.")]
        public static partial void BucketReady(this ILogger logger, string key, int count);

        [LoggerMessage(EventId = 3008, Level = LogLevel.Warning, Message = "Error processing bucket {key} with ID {id}, will retry later.")]
        public static partial void BucketError(this ILogger logger, string key, Guid id, Exception ex);

        [LoggerMessage(EventId = 3009, Level = LogLevel.Warning, Message = "Error removing bucket {key}.")]
        public static partial void BucketRemoveError(this ILogger logger, string key);

        [LoggerMessage(EventId = 3010, Level = LogLevel.Debug, Message = "Bucket not found.")]
        public static partial void BucketNotFound(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 3011, Level = LogLevel.Error, Message = "Error while processing buckets/payloads.")]
        public static partial void ErrorProcessingBuckets(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 3012, Level = LogLevel.Information, Message = "Bucket {key} created with timeout {timeout}s.")]
        public static partial void BucketCreated(this ILogger logger, string key, uint timeout);
    }
}
