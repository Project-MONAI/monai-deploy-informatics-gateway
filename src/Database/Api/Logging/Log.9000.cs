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

namespace Monai.Deploy.InformaticsGateway.Database.Api.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 9000, Level = LogLevel.Error, Message = "Error adding item {type} to the database.")]
        public static partial void ErrorAddItem(this ILogger logger, string @type, Exception ex);

        [LoggerMessage(EventId = 9001, Level = LogLevel.Error, Message = "Error updating item {type} in the database.")]
        public static partial void ErrorUpdateItem(this ILogger logger, string @type, Exception ex);

        [LoggerMessage(EventId = 9002, Level = LogLevel.Error, Message = "Error deleting item {type} from the database.")]
        public static partial void ErrorDeleteItem(this ILogger logger, string @type, Exception ex);

        [LoggerMessage(EventId = 9003, Level = LogLevel.Error, Message = "Error performing database action. Waiting {timespan} before next retry. Retry attempt {retryCount}...")]
        public static partial void DatabaseErrorRetry(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 9004, Level = LogLevel.Debug, Message = "Storage metadata saved to the database.")]
        public static partial void StorageMetadataSaved(this ILogger logger);

        [LoggerMessage(EventId = 9005, Level = LogLevel.Error, Message = "Error querying pending inference request.")]
        public static partial void ErrorQueryingForPendingInferenceRequest(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 9006, Level = LogLevel.Debug, Message = "Inference request saved.")]
        public static partial void InferenceRequestSaved(this ILogger logger);

        [LoggerMessage(EventId = 9007, Level = LogLevel.Warning, Message = "Exceeded maximum retries.")]
        public static partial void InferenceRequestUpdateExceededMaximumRetries(this ILogger logger);

        [LoggerMessage(EventId = 9008, Level = LogLevel.Information, Message = "Failed to process inference request, will retry later.")]
        public static partial void InferenceRequestUpdateRetryLater(this ILogger logger);

        [LoggerMessage(EventId = 9009, Level = LogLevel.Debug, Message = "Updating request {transactionId} to InProgress.")]
        public static partial void InferenceRequestSetToInProgress(this ILogger logger, string transactionId);

        [LoggerMessage(EventId = 9010, Level = LogLevel.Debug, Message = "Updating inference request.")]
        public static partial void InferenceRequestUpdateState(this ILogger logger);

        [LoggerMessage(EventId = 9011, Level = LogLevel.Information, Message = "Inference request updated.")]
        public static partial void InferenceRequestUpdated(this ILogger logger);
    }
}
