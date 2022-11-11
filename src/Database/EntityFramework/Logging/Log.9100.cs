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

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 9100, Level = LogLevel.Error, Message = "Error performing database action. Waiting {timespan} before next retry. Retry attempt {retryCount}...")]
        public static partial void DatabaseErrorRetry(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 9101, Level = LogLevel.Debug, Message = "Storage metadata saved to the database.")]
        public static partial void StorageMetadataSaved(this ILogger logger);

        [LoggerMessage(EventId = 9102, Level = LogLevel.Error, Message = "Error querying pending inference request.")]
        public static partial void ErrorQueryingForPendingInferenceRequest(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 9103, Level = LogLevel.Debug, Message = "Inference request saved.")]
        public static partial void InferenceRequestSaved(this ILogger logger);

        [LoggerMessage(EventId = 9104, Level = LogLevel.Warning, Message = "Exceeded maximum retries.")]
        public static partial void InferenceRequestUpdateExceededMaximumRetries(this ILogger logger);

        [LoggerMessage(EventId = 9105, Level = LogLevel.Information, Message = "Failed to process inference request, will retry later.")]
        public static partial void InferenceRequestUpdateRetryLater(this ILogger logger);

        [LoggerMessage(EventId = 9106, Level = LogLevel.Debug, Message = "Updating request {transactionId} to InProgress.")]
        public static partial void InferenceRequestSetToInProgress(this ILogger logger, string transactionId);

        [LoggerMessage(EventId = 9107, Level = LogLevel.Debug, Message = "Updating inference request.")]
        public static partial void InferenceRequestUpdateState(this ILogger logger);

        [LoggerMessage(EventId = 9108, Level = LogLevel.Information, Message = "Inference request updated.")]
        public static partial void InferenceRequestUpdated(this ILogger logger);
    }
}
