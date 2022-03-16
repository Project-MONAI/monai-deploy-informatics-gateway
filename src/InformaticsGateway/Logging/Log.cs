// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{ServiceName} started.")]
        public static partial void ServiceStarted(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{ServiceName} is stopping.")]
        public static partial void ServiceStopping(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "{ServiceName} is canceled.")]
        public static partial void ServiceCancelled(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "{ServiceName} is canceled.")]
        public static partial void ServiceCancelledWithException(this ILogger logger, string serviceName, Exception ex);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "{ServiceName} may be disposed.")]
        public static partial void ServiceDisposed(this ILogger logger, string serviceName, Exception ex);

        [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "{ServiceName} is running.")]
        public static partial void ServiceRunning(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Waiting for {ServiceName} to stop.")]
        public static partial void ServiceStopPending(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Error querying database.")]
        public static partial void ErrorQueryingDatabase(this ILogger logger, Exception ex);

        // Application Entity Manager
        [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "ApplicationEntityManager stopping.")]
        public static partial void ApplicationEntityManagerStopping(this ILogger logger);

        [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Study Instance UID: {StudyInstanceUid}. Series Instance UID: {SeriesInstanceUid}. Storage File Path: {InstanceStorageFullPath}.")]
        public static partial void InstanceInformation(this ILogger logger, string studyInstanceUid, string seriesInstanceUid, string instanceStorageFullPath);

        [LoggerMessage(EventId = 102, Level = LogLevel.Error, Message = "AE Title {AETitle} could not be added to CStore Manager.  Already exits: {exists}.")]
        public static partial void AeTitleCannotBeAdded(this ILogger logger, string aeTitle, bool exists);

        [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "{AETitle} added to AE Title Manager.")]
        public static partial void AeTitleAdded(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "Error notifying observer.")]
        public static partial void ErrorNotifyingObserver(this ILogger logger);

        [LoggerMessage(EventId = 105, Level = LogLevel.Information, Message = "{aeTitle} removed from AE Title Manager.")]
        public static partial void AeTitleRemoved(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 106, Level = LogLevel.Information, Message = "Available source AET: {aeTitle} @ {hostIp}.")]
        public static partial void AvailableSource(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 107, Level = LogLevel.Information, Message = "Loading MONAI Application Entities from data store.")]
        public static partial void LoadingMonaiAeTitles(this ILogger logger);

        // Export Services
        [LoggerMessage(EventId = 500, Level = LogLevel.Information, Message = "{ServiceName} subscribed to {RoutingKey} messages.")]
        public static partial void ExportEventSubscription(this ILogger logger, string serviceName, string routingKey);

        [LoggerMessage(EventId = 501, Level = LogLevel.Warning, Message = "{ServiceName} paused due to insufficient storage space.  Available storage space: {availableFreeSpace:D}.")]
        public static partial void ExportPausedDueToInsufficientStorageSpace(this ILogger logger, string serviceName, long availableFreeSpace);

        [LoggerMessage(EventId = 502, Level = LogLevel.Warning, Message = "The export request {exportTaskId} is already queued for export.")]
        public static partial void ExportRequestAlreadyQueued(this ILogger logger, string exportTaskId);

        [LoggerMessage(EventId = 503, Level = LogLevel.Debug, Message = "Downloading {file}.")]
        public static partial void DownloadingFile(this ILogger logger, string file);

        [LoggerMessage(EventId = 504, Level = LogLevel.Debug, Message = "File {file} ready for export.")]
        public static partial void FileReadyForExport(this ILogger logger, string file);

        [LoggerMessage(EventId = 505, Level = LogLevel.Information, Message = "Export task completed with {failedCount} failures out of {fileCount}.")]
        public static partial void ExportCompleted(this ILogger logger, int failedCount, int fileCount);

        [LoggerMessage(EventId = 506, Level = LogLevel.Error, Message = "Error downloading payload. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorDownloadingPayloadWithRetry(this ILogger logger, Exception ex, TimeSpan timeSpan, int retryCount);

        [LoggerMessage(EventId = 507, Level = LogLevel.Error, Message = "Error downloading payload.")]
        public static partial void ErrorDownloadingPayload(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 508, Level = LogLevel.Error, Message = "Error acknowledging message. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorAcknowledgingMessageWithRetry(this ILogger logger, Exception ex, TimeSpan timeSpan, int retryCount);

        [LoggerMessage(EventId = 509, Level = LogLevel.Information, Message = "Sending acknowledgement.")]
        public static partial void SendingAckowledgement(this ILogger logger);

        [LoggerMessage(EventId = 510, Level = LogLevel.Error, Message = "Error publishing message. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorPublishingExportCompleteMessageWithRetry(this ILogger logger, Exception ex, TimeSpan timeSpan, int retryCount);

        [LoggerMessage(EventId = 511, Level = LogLevel.Information, Message = "Publishing export complete message.")]
        public static partial void PublishingExportCompleteEvent(this ILogger logger);

        [LoggerMessage(EventId = 512, Level = LogLevel.Debug, Message = "Calling ReportActionCompleted callback.")]
        public static partial void CallingReportActionCompletedCallback(this ILogger logger);

        // Data Retrieval Service
        [LoggerMessage(EventId = 600, Level = LogLevel.Information, Message = "Processing input source '{inputInterface}' from {uri}.")]
        public static partial void ProcessingInputResource(this ILogger logger, InputInterfaceType inputInterface, string uri);

        [LoggerMessage(EventId = 601, Level = LogLevel.Warning, Message = "Specified input interface is not supported '{inputInterface}`.")]
        public static partial void UnsupportedInputInterface(this ILogger logger, InputInterfaceType inputInterface);

        [LoggerMessage(EventId = 602, Level = LogLevel.Debug, Message = "Restoring previously retrieved DICOM instances from {storagePath}.")]
        public static partial void RestoringRetrievedFiles(this ILogger logger, string storagePath);

        [LoggerMessage(EventId = 603, Level = LogLevel.Debug, Message = "Restored previously retrieved file {file}.")]
        public static partial void RestoredFile(this ILogger logger, string file);

        [LoggerMessage(EventId = 604, Level = LogLevel.Error, Message = "Error retrieving FHIR resource {type}/{id}.")]
        public static partial void ErrorRetrievingFhirResource(this ILogger logger, string type, string id, Exception ex);

        [LoggerMessage(EventId = 605, Level = LogLevel.Error, Message = "Failed to retrieve resource {type}/{id} with status code {statusCode}, retry count={retryCount}.")]
        public static partial void ErrorRetrievingFhirResourceWithRetry(this ILogger logger, string type, string id, HttpStatusCode statusCode, int retryCount, Exception ex);

        [LoggerMessage(EventId = 606, Level = LogLevel.Error, Message = "Error retriving FHIR resource {type}/{id}. Recevied HTTP status code {statusCode}.")]
        public static partial void ErrorRetrievingFhirResourceWithStatus(this ILogger logger, string type, string id, HttpStatusCode statusCode);

        [LoggerMessage(EventId = 607, Level = LogLevel.Debug, Message = "Retriving FHIR resource {type}/{id} with media format {acceptHeader} and file format {fhirFormat}.")]
        public static partial void RetrievingFhirResource(this ILogger logger, string type, string id, string acceptHeader, FhirStorageFormat fhirFormat);

        [LoggerMessage(EventId = 608, Level = LogLevel.Information, Message = "Performing QIDO with {dicomTag}={queryValue}.")]
        public static partial void PerformQido(this ILogger logger, string dicomTag, string queryValue);

        [LoggerMessage(EventId = 609, Level = LogLevel.Debug, Message = "Study {studyInstanceUid} found with QIDO query {dicomTag}={queryValue}.")]
        public static partial void StudyFoundWithQido(this ILogger logger, string studyInstanceUid, string dicomTag, string queryValue);

        [LoggerMessage(EventId = 610, Level = LogLevel.Warning, Message = "Instance {instance} does not contain StudyInstanceUid.")]
        public static partial void InstanceMissingStudyInstanceUid(this ILogger logger, string instance);

        [LoggerMessage(EventId = 611, Level = LogLevel.Warning, Message = "No studies found with specified query parameter {dicomTag}={queryValue}.")]
        public static partial void QidoCompletedWithNoResult(this ILogger logger, string dicomTag, string queryValue);

        [LoggerMessage(EventId = 612, Level = LogLevel.Information, Message = "Retrieving study {studyInstanceUid}")]
        public static partial void RetrievingStudyWithWado(this ILogger logger, string studyInstanceUid);

        [LoggerMessage(EventId = 613, Level = LogLevel.Information, Message = "Retrieving series {seriesInstanceUid}")]
        public static partial void RetrievingSeriesWithWado(this ILogger logger, string seriesInstanceUid);

        [LoggerMessage(EventId = 614, Level = LogLevel.Information, Message = "Retrieving instance {sopInstanceUid}")]
        public static partial void RetrievingInstanceWithWado(this ILogger logger, string sopInstanceUid);

        [LoggerMessage(EventId = 615, Level = LogLevel.Warning, Message = "Instance '{file}' already retrieved/stored.")]
        public static partial void InstanceAlreadyExists(this ILogger logger, string file);

        [LoggerMessage(EventId = 616, Level = LogLevel.Error, Message = "Failed to save instance '{file}', retry count={retryCount}.")]
        public static partial void ErrorSavingInstance(this ILogger logger, string file, int retryCount, Exception ex);

        [LoggerMessage(EventId = 617, Level = LogLevel.Debug, Message = "Saving DICOM instance {path}..")]
        public static partial void SavingInstance(this ILogger logger, string path);

        [LoggerMessage(EventId = 618, Level = LogLevel.Information, Message = "Instance saved successfully {path}.")]
        public static partial void InstanceSaved(this ILogger logger, string path);

        [LoggerMessage(EventId = 619, Level = LogLevel.Warning, Message = "Data retrieval paused due to insufficient storage space.Available storage space: {space:D}.")]
        public static partial void DataRetrievalPaused(this ILogger logger, long space);

        [LoggerMessage(EventId = 620, Level = LogLevel.Information, Message = "Processing inference request.")]
        public static partial void ProcessingInferenceRequest(this ILogger logger);

        [LoggerMessage(EventId = 621, Level = LogLevel.Information, Message = "Inference request completed and ready for job submission.")]
        public static partial void InferenceRequestProcessed(this ILogger logger);

        [LoggerMessage(EventId = 622, Level = LogLevel.Error, Message = "Error processing request: TransactionId = {transactionId}.")]
        public static partial void ErrorProcessingInferenceRequest(this ILogger logger, string transactionId, Exception ex);

        // Payload Service

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

        // MONAI AE TItle Controller

        [LoggerMessage(EventId = 800, Level = LogLevel.Error, Message = "Error querying MONAI Application Entity.")]
        public static partial void ErrorListingMonaiApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 801, Level = LogLevel.Error, Message = "Error adding MONAI Application Entity.")]
        public static partial void ErrorAddingMonaiApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 802, Level = LogLevel.Error, Message = "Error deleting MONAI Application Entity.")]
        public static partial void ErrorDeletingMonaiApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 803, Level = LogLevel.Information, Message = "MONAI SCP AE Title added AE Title={aeTitle}.")]
        public static partial void MonaiApplicationEntityAdded(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 804, Level = LogLevel.Information, Message = "MONAI SCP Application Entity deleted {name}.")]
        public static partial void MonaiApplicationEntityDeleted(this ILogger logger, string name);

        // Destination AE Title Controller
        [LoggerMessage(EventId = 900, Level = LogLevel.Information, Message = "DICOM destination added AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void DestinationApplicationEntityAdded(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 901, Level = LogLevel.Information, Message = "MONAI SCP Application Entity deleted {name}.")]
        public static partial void DestinationApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 902, Level = LogLevel.Error, Message = "Error querying DICOM destinations.")]
        public static partial void ErrorListingDestinationApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 903, Level = LogLevel.Error, Message = "Error adding new DICOM destination.")]
        public static partial void ErrorAddingDestinationApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 904, Level = LogLevel.Error, Message = "Error deleting DICOM destination.")]
        public static partial void ErrorDeletingDestinationApplicationEntity(this ILogger logger, Exception ex);

        // Source AE Title Controller
        [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "DICOM source added AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void SourceApplicationEntityAdded(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "DICOM source deleted {name}.")]
        public static partial void SourceApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Error querying DICOM sources.")]
        public static partial void ErrorListingSourceApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Error adding new DICOM source.")]
        public static partial void ErrorAddingSourceApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Error deleting DICOM source.")]
        public static partial void ErrorDeletingSourceApplicationEntity(this ILogger logger, Exception ex);

    }
}
