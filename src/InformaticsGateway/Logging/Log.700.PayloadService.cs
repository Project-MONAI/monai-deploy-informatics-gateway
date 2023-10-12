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
using Monai.Deploy.InformaticsGateway.Api.Storage;
using static Monai.Deploy.InformaticsGateway.Api.Storage.Payload;

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

        [LoggerMessage(EventId = 704, Level = LogLevel.Information, Message = "Moving files in payload {payloadId} to storage service at {bucketName}.")]
        public static partial void MovingFIlesInPayload(this ILogger logger, Guid payloadId, string bucketName);

        [LoggerMessage(EventId = 706, Level = LogLevel.Warning, Message = "Failed to publish workflow request for payload {payloadId}; added back to queue for retry.")]
        public static partial void FailedToPublishWorkflowRequest(this ILogger logger, Guid payloadId, Exception ex);

        [LoggerMessage(EventId = 707, Level = LogLevel.Error, Message = "Reached maximum number of retries for moving files in the payload {payloadId}, giving up.")]
        public static partial void MoveFailureStopRetry(this ILogger logger, Guid payloadId, Exception ex);

        [LoggerMessage(EventId = 708, Level = LogLevel.Error, Message = "Move failure. Updating payload {payloadId} state={state}, retries={retryCount}.")]
        public static partial void MoveFailureRetryLater(this ILogger logger, Guid payloadId, Payload.PayloadState state, int retryCount, Exception ex);

        [LoggerMessage(EventId = 709, Level = LogLevel.Error, Message = "Error updating payload failure: Payload = {payloadId}.")]
        public static partial void ErrorUpdatingPayload(this ILogger logger, Guid? payloadId, Exception ex);

        [LoggerMessage(EventId = 710, Level = LogLevel.Debug, Message = "Generating workflow request message for payload {payloadId}...")]
        public static partial void GenerateWorkflowRequest(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 711, Level = LogLevel.Information, Message = "Publishing workflow request message ID={messageId}...")]
        public static partial void PublishingWorkflowRequest(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 712, Level = LogLevel.Information, Message = "Workflow request published to {queue}, message ID={messageId}. Payload took {durationSeconds} seconds to complete.")]
        public static partial void WorkflowRequestPublished(this ILogger logger, string queue, string messageId, double durationSeconds);

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

        [LoggerMessage(EventId = 720, Level = LogLevel.Information, Message = "Payload {id} deleted.")]
        public static partial void PayloadDeleted(this ILogger logger, Guid id);

        [LoggerMessage(EventId = 722, Level = LogLevel.Debug, Message = "Copy files statistics: {threads} threads, {seconds} seconds.")]
        public static partial void CopyStats(this ILogger logger, int threads, double seconds);

        [LoggerMessage(EventId = 724, Level = LogLevel.Debug, Message = "Moving temporary file to payload {payloadId} directory {destination} on storage service.")]
        public static partial void MovingFileToPayloadDirectory(this ILogger logger, Guid payloadId, string destination);

        [LoggerMessage(EventId = 727, Level = LogLevel.Debug, Message = "Deleting temporary file {identifier} from temporary bucket {bucket} at {remotePath}.")]
        public static partial void DeletingFileFromTemporaryBbucket(this ILogger logger, string bucket, string identifier, string remotePath);

        [LoggerMessage(EventId = 728, Level = LogLevel.Error, Message = "Reached maximum number of retries for notifying payload {payloadId} ready, giving up.")]
        public static partial void NotificationFailureStopRetry(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 729, Level = LogLevel.Error, Message = "Notification failure. Updating payload {payloadId} state={state}, retries={retryCount}.")]
        public static partial void NotificationFailureRetryLater(this ILogger logger, Guid payloadId, Api.Storage.Payload.PayloadState state, int retryCount);

        [LoggerMessage(EventId = 730, Level = LogLevel.Error, Message = "Unknown error occurred while moving files in the payload.")]
        public static partial void FailedToMoveFilesInPayloadUknownError(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 731, Level = LogLevel.Error, Message = "Unable to move files in the payload due to incorrect payload state {payloadState}.")]
        public static partial void FailedToMoveFilesInPayloadIncorrectState(this ILogger logger, PayloadState payloadState, Exception ex);

        [LoggerMessage(EventId = 732, Level = LogLevel.Error, Message = "Error deleted payload {payloadId} associated storage metadata object.  Waiting {timespan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorDeletingPayloadAssociatedStorageMetadataObjects(this ILogger logger, Guid payloadId, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 733, Level = LogLevel.Debug, Message = "Storage metadata object {identifier} deleted.")]
        public static partial void StorageMetadataObjectDeleted(this ILogger logger, string identifier);

        [LoggerMessage(EventId = 734, Level = LogLevel.Error, Message = "File {destinationPath} in payload {payloadId} moved but cannot be found on the storage service.")]
        public static partial void FileMovedVerificationFailure(this ILogger logger, Guid payloadId, string destinationPath);

        [LoggerMessage(EventId = 735, Level = LogLevel.Trace, Message = "File already moved to {destinationPath} in payload {payloadId}.")]
        public static partial void AlreadyMoved(this ILogger logger, Guid payloadId, string destinationPath);

        [LoggerMessage(EventId = 736, Level = LogLevel.Debug, Message = "Failed to delete temporary file {identifier} from temporary bucket {bucket} at {remotePath}.")]
        public static partial void ErrorDeletingFileAfterMoveComplete(this ILogger logger, string bucket, string identifier, string remotePath);

        [LoggerMessage(EventId = 737, Level = LogLevel.Trace, Message = "File found on storage service {bucket}: {filePaths}.")]
        public static partial void FileFounddOnStorageService(this ILogger logger, string bucket, string filePaths);

        [LoggerMessage(EventId = 738, Level = LogLevel.Error, Message = "Error listing files on storage service.")]
        public static partial void ErrorListingFilesOnStorageService(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 739, Level = LogLevel.Trace, Message = "Total number of files found on storage service {bucket}: {count}.")]
        public static partial void FilesFounddOnStorageService(this ILogger logger, string bucket, int count);

        [LoggerMessage(EventId = 740, Level = LogLevel.Error, Message = "Some or all files were missing in payload {payloadId}, will abort the request.")]
        public static partial void DeletePayloadDueToMissingFiles(this ILogger logger, Guid payloadId, Exception ex);

        [LoggerMessage(EventId = 741, Level = LogLevel.Error, Message = "File {file} not found in {payloadId}.")]
        public static partial void FileMissingInPayload(this ILogger logger, Guid payloadId, string file, Exception ex);

        [LoggerMessage(EventId = 742, Level = LogLevel.Critical, Message = "Storage service connection error.")]
        public static partial void StorageServiceConnectionError(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 743, Level = LogLevel.Error, Message = "Exception moving payload.")]
        public static partial void PayloadMoveException(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 744, Level = LogLevel.Warning, Message = "PayloadNotification move payload queue: faulted: {isFauled}, cancelled: {isCancelled}.")]
        public static partial void MoveQueueFaulted(this ILogger logger, bool isFauled, bool isCancelled);

        [LoggerMessage(EventId = 745, Level = LogLevel.Warning, Message = "PayloadNotification publishing payload queue: faulted: {isFauled}, cancelled: {isCancelled}.")]
        public static partial void PublishQueueFaulted(this ILogger logger, bool isFauled, bool isCancelled);

        [LoggerMessage(EventId = 746, Level = LogLevel.Error, Message = "Error posting payload to move queue.")]
        public static partial void ErrorPostingJobToMovePayloadsQueue(this ILogger logger);

        [LoggerMessage(EventId = 747, Level = LogLevel.Error, Message = "Error posting payload to publish queue.")]
        public static partial void ErrorPostingJobToPublishPayloadsQueue(this ILogger logger);

        [LoggerMessage(EventId = 748, Level = LogLevel.Debug, Message = "Generating artifact recieeved request message for payload {payloadId}...")]
        public static partial void GenerateArtifactReceievedRequest(this ILogger logger, Guid payloadId);

        [LoggerMessage(EventId = 749, Level = LogLevel.Information, Message = "Publishing artifact recieved request message ID={messageId}...")]
        public static partial void PublishingArtifactRecievedRequest(this ILogger logger, string messageId);

        [LoggerMessage(EventId = 750, Level = LogLevel.Information, Message = "Artifact recieved published to {queue}, message ID={messageId}. Payload took {durationSeconds} seconds to complete.")]
        public static partial void ArtifactRecievedPublished(this ILogger logger, string queue, string messageId, double durationSeconds);
    }
}
