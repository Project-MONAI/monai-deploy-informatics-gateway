/*
 * Copyright 2022-2023 MONAI Consortium
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
        [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "[Startup] Removing payloads from database.")]
        public static partial void RemovingPendingPayloads(this ILogger logger);

        [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "[Startup] {count} pending payloads removed from database.")]
        public static partial void TotalNumberOfPayloadsRemoved(this ILogger logger, int count);

        [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "File added to bucket {key}. Queue size: {count}")]
        public static partial void FileAddedToBucket(this ILogger logger, string key, int count);

        [LoggerMessage(EventId = 3004, Level = LogLevel.Trace, Message = "Number of incomplete payloads waiting for processing: {count}.")]
        public static partial void BucketsActive(this ILogger logger, int count);

        [LoggerMessage(EventId = 3005, Level = LogLevel.Trace, Message = "Checking elapsed time for bucket: {key} with timeout set to {timeout}s. Elapsed {elapsed}s with {succeededFiles} uplaoded and {failedFiles} failures  out of {totalNumberOfFiles}.")]
        public static partial void BucketElapsedTime(this ILogger logger, string key, uint timeout, double elapsed, int totalNumberOfFiles, int succeededFiles, int failedFiles);

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

        [LoggerMessage(EventId = 3014, Level = LogLevel.Error, Message = "Payload ({key}) with {totalNumberOfFiles} files deleted due to {failures} upload failure(s).")]
        public static partial void PayloadRemovedWithFailureUploads(this ILogger logger, string key, int totalNumberOfFiles, int failures);

        [LoggerMessage(EventId = 3015, Level = LogLevel.Information, Message = "Receieved a payload with {totalNumberOfFiles} files and {failedFiles} failures in {elapsed} seconds.")]
        public static partial void ReceievedAPayload(this ILogger logger, double elapsed, int totalNumberOfFiles, int failedFiles);
    }
}
