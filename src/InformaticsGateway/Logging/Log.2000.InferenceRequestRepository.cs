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
        [LoggerMessage(EventId = 2000, Level = LogLevel.Error, Message = "Error saving inference request. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorSavingInferenceRequest(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "Inference request saved.")]
        public static partial void InferenceRequestSaved(this ILogger logger);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Exceeded maximum retries.")]
        public static partial void InferenceRequestUpdateExceededMaximumRetries(this ILogger logger);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Will retry later.")]
        public static partial void InferenceRequestUpdateRetryLater(this ILogger logger);

        [LoggerMessage(EventId = 2004, Level = LogLevel.Debug, Message = "Updating request {transactionId} to InProgress.")]
        public static partial void InferenceRequestSetToInProgress(this ILogger logger, string transactionId);

        [LoggerMessage(EventId = 2005, Level = LogLevel.Debug, Message = "Updating inference request.")]
        public static partial void InferenceRequestUpdateState(this ILogger logger);

        [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Inference request updated.")]
        public static partial void InferenceRequestUpdated(this ILogger logger);

        [LoggerMessage(EventId = 2007, Level = LogLevel.Error, Message = "Error while updating inference request. Waiting {timespan} before next retry. Retry attempt {retryCount}...")]
        public static partial void InferenceRequestUpdateError(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);
    }
}
