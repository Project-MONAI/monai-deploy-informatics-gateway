// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 700, Level = LogLevel.Error, Message = "Error processing request: Payload = {payloadId}.")]
        public static partial void ErrorProcessingPayload(this ILogger logger, Guid? payloadId, Exception ex);

        [LoggerMessage(EventId = 701, Level = LogLevel.Information, Message = "Payload {payloadId} added to {serviceName} for processing.")]
        public static partial void PayloadQueuedForProcessing(this ILogger logger, Guid payloadId, string serviceName);

        [LoggerMessage(EventId = 702, Level = LogLevel.Information, Message = "Payload {payloadId} ready to be published.")]
        public static partial void PayloadReadyToBePublished(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 703, Level = LogLevel.Warning, Message = "Failed to upload payload {payloadId}; added back to queue for retry.")]
        public static partial void FailedToUpload(this ILogger logger, Guid payloadId, Exception ex);

        [LoggerMessage(EventId = 704, Level = LogLevel.Information, Message = "Uploading payload {payloadId} to storage service at {bucketName}.")]
        public static partial void UploadingPayloadToBucket(this ILogger logger, Guid payloadId, string bucketName);

        [LoggerMessage(EventId = 705, Level = LogLevel.Debug, Message = "Uploading file {filePath} from payload {payloadId} to storage service.")]
        public static partial void UploadingFileInPayload(this ILogger logger, Guid payloadId, string filePath);

        [LoggerMessage(EventId = 706, Level = LogLevel.Warning, Message = "Failed to publish workflow request for payload {payloadId}; added back to queue for retry.")]
        public static partial void FailedToPublishWorkflowRequest(this ILogger logger, Guid payloadId, Exception ex);

        [LoggerMessage(EventId = 707, Level = LogLevel.Error, Message = "Reached maximum number of retries for payload {payloadId}, giving up.")]
        public static partial void UploadFailureStopRetry(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 708, Level = LogLevel.Error, Message = "Updating payload {payloadId} state={state}, retries={retryCount}.")]
        public static partial void UploadFailureRetryLater(this ILogger logger, Guid payloadId, Api.Storage.Payload.PayloadState state, int retryCount);

        [LoggerMessage(EventId = 709, Level = LogLevel.Error, Message = "Error updating payload failure: Payload = {payloadId}.")]
        public static partial void ErrorUpdatingPayload(this ILogger logger, Guid? payloadId, Exception ex);

        [LoggerMessage(EventId = 710, Level = LogLevel.Debug, Message = "Generating workflow request message for payload {payloadId}...")]
        public static partial void GenerateWorkflowRequest(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 711, Level = LogLevel.Information, Message = "Publishing workflow request message ID={messageId}...")]
        public static partial void PublishingWorkflowRequest(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 712, Level = LogLevel.Information, Message = "Workflow request published to {queue}, message ID={messageId}.")]
        public static partial void WorkflowRequestPublished(this ILogger logger, string queue, string messageId);

        [LoggerMessage(EventId = 713, Level = LogLevel.Information, Message = "Restoring payloads from database.")]
        public static partial void StartupRestoreFromDatabase(this ILogger logger);

        [LoggerMessage(EventId = 714, Level = LogLevel.Information, Message = "{count} payloads restored from database.")]
        public static partial void RestoredFromDatabase(this ILogger logger, int count);

        [LoggerMessage(EventId = 715, Level = LogLevel.Error, Message = "Error saving payload. Waiting {timespan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorSavingPayload(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 716, Level = LogLevel.Debug, Message = "Payload {id} saved.")]
        public static partial void PayloadSaved(this ILogger logger, Guid id);

        [LoggerMessage(EventId = 717, Level = LogLevel.Error, Message = "Error adding payload. Waiting {timespan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorAddingPayload(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 718, Level = LogLevel.Debug, Message = "Payload {id} added.")]
        public static partial void PayloadAdded(this ILogger logger, Guid id);

        [LoggerMessage(EventId = 719, Level = LogLevel.Error, Message = "Error deleting payload. Waiting {timespan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorDeletingPayload(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 720, Level = LogLevel.Debug, Message = "Payload {id} deleted.")]
        public static partial void PayloadDeleted(this ILogger logger, Guid id);

        [LoggerMessage(EventId = 721, Level = LogLevel.Debug, Message = "Upload statistics: {threads} threads, {seconds} seconds.")]
        public static partial void UploadStats(this ILogger logger, int threads, double seconds);
    }
}
