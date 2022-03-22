// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Services.Scp;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{ServiceName} started.")]
        public static partial void ServiceStarted(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{ServiceName} is stopping.")]
        public static partial void ServiceStopping(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "{ServiceName} canceled.")]
        public static partial void ServiceCancelled(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "{ServiceName} canceled.")]
        public static partial void ServiceCancelledWithException(this ILogger logger, string serviceName, Exception ex);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "{ServiceName} may be disposed.")]
        public static partial void ServiceDisposed(this ILogger logger, string serviceName, Exception ex);

        [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "{ServiceName} is running.")]
        public static partial void ServiceRunning(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Waiting for {ServiceName} to stop.")]
        public static partial void ServiceStopPending(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Error querying database.")]
        public static partial void ErrorQueryingDatabase(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 9, Level = LogLevel.Critical, Message = "Type '{type}' cannot be found.")]
        public static partial void TypeNotFound(this ILogger logger, string type);

        [LoggerMessage(EventId = 10, Level = LogLevel.Critical, Message = "Instance of '{type}' cannot be found.")]
        public static partial void InstanceOfTypeNotFound(this ILogger logger, string type);

        [LoggerMessage(EventId = 11, Level = LogLevel.Critical, Message = "Instance of '{type}' cannot be found.")]
        public static partial void ServiceInvalidOrCancelled(this ILogger logger, string type, Exception ex);

        [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "{ServiceName} starting.")]
        public static partial void ServiceStarting(this ILogger logger, string serviceName);

        [LoggerMessage(EventId = 13, Level = LogLevel.Critical, Message = "Failed to start {ServiceName}.")]
        public static partial void ServiceFailedToStart(this ILogger logger, string serviceName, Exception ex);

        // Application Entity Manager/Handler
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

        [LoggerMessage(EventId = 108, Level = LogLevel.Information, Message = "Instance ignored due to matching SOP Class UID {uid}.")]
        public static partial void InstanceIgnoredWIthMatchingSopClassUid(this ILogger logger, string uid);

        [LoggerMessage(EventId = 109, Level = LogLevel.Information, Message = "Queuing instance with group {dicomTag}.")]
        public static partial void QueueInstanceUsingDicomTag(this ILogger logger, DicomTag dicomTag);

        [LoggerMessage(EventId = 110, Level = LogLevel.Debug, Message = "Saving instance {filename}.")]
        public static partial void AESavingInstance(this ILogger logger, string filename);

        [LoggerMessage(EventId = 111, Level = LogLevel.Information, Message = "Instance saved {filename}.")]
        public static partial void AEInstanceSaved(this ILogger logger, string filename);

        [LoggerMessage(EventId = 112, Level = LogLevel.Information, Message = "Notifying {count} observers of MONAI Application Entity {eventType}.")]
        public static partial void NotifyAeChanged(this ILogger logger, int count, ChangedEventType eventType);

        // SCP Service
        [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "MONAI Deploy Informatics Gateway (SCP Service) {version} loading...")]
        public static partial void ScpServiceLoading(this ILogger logger, string version);

        [LoggerMessage(EventId = 201, Level = LogLevel.Critical, Message = "Failed to initialize SCP listener.")]
        public static partial void ScpListenerInitializationFailure(this ILogger logger);

        [LoggerMessage(EventId = 202, Level = LogLevel.Information, Message = "SCP listening on port: {port}.")]
        public static partial void ScpListeningOnPort(this ILogger logger, int port);

        [LoggerMessage(EventId = 203, Level = LogLevel.Information, Message = "C-ECHO request received.")]
        public static partial void CEchoReceived(this ILogger logger);

        [LoggerMessage(EventId = 204, Level = LogLevel.Error, Message = "Connection closed with exception.")]
        public static partial void ConnectionClosedWithException(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 205, Level = LogLevel.Information, Message = "Transfer syntax used: {transferSyntax}.")]
        public static partial void TransferSyntaxUsed(this ILogger logger, DicomTransferSyntax transferSyntax);

        [LoggerMessage(EventId = 206, Level = LogLevel.Error, Message = "Failed to process C-STORE request, out of storage space.")]
        public static partial void CStoreFailedWithNoSpace(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 207, Level = LogLevel.Error, Message = "Failed to process C-STORE request.")]
        public static partial void CStoreFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 208, Level = LogLevel.Warning, Message = "Aborted {source} with reason {reason}.")]
        public static partial void CStoreAbort(this ILogger logger, DicomAbortSource source, DicomAbortReason reason);

        [LoggerMessage(EventId = 209, Level = LogLevel.Information, Message = "Association release request received.")]
        public static partial void CStoreAssociationReleaseRequest(this ILogger logger);

        [LoggerMessage(EventId = 210, Level = LogLevel.Information, Message = "Association received from {host}:{port}.")]
        public static partial void CStoreAssociationReceived(this ILogger logger, string host, int port);

        [LoggerMessage(EventId = 211, Level = LogLevel.Warning, Message = "Verification service is disabled: rejecting association.")]
        public static partial void VerificationServiceDisabled(this ILogger logger);

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

        [LoggerMessage(EventId = 513, Level = LogLevel.Error, Message = "The specified inference request '{destination}' cannot be found and will not be exported.")]
        public static partial void InferenceRequestExportDestinationNotFound(this ILogger logger, string destination);

        [LoggerMessage(EventId = 514, Level = LogLevel.Error, Message = "The inference request contains no `outputResources` nor any DICOMweb export destinations.")]
        public static partial void InferenceRequestExportNoDestinationNotFound(this ILogger logger);

        [LoggerMessage(EventId = 515, Level = LogLevel.Debug, Message = "Exporting data to {uri}.")]
        public static partial void ExportToDicomWeb(this ILogger logger, string uri);

        [LoggerMessage(EventId = 516, Level = LogLevel.Error, Message = "Error exporting to DICOMweb destination {uri}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorExportingDicomWebWithRetry(this ILogger logger, string uri, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 517, Level = LogLevel.Information, Message = "All data exported successfully.")]
        public static partial void ExportSuccessfully(this ILogger logger);

        [LoggerMessage(EventId = 518, Level = LogLevel.Error, Message = "Error occurred while exporting.")]
        public static partial void ErrorExporting(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 519, Level = LogLevel.Error, Message = "Error processing export task.")]
        public static partial void ErrorProcessingExportTask(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 520, Level = LogLevel.Error, Message = "SCU Export configuration error: {message}")]
        public static partial void ScuExportConfigurationError(this ILogger logger, string message, Exception ex);

        [LoggerMessage(EventId = 521, Level = LogLevel.Information, Message = "Association accepted.")]
        public static partial void AssociationAccepted(this ILogger logger);

        [LoggerMessage(EventId = 522, Level = LogLevel.Warning, Message = "Association rejected.")]
        public static partial void AssociationRejected(this ILogger logger);

        [LoggerMessage(EventId = 523, Level = LogLevel.Information, Message = "Association released.")]
        public static partial void AssociationReleased(this ILogger logger);

        [LoggerMessage(EventId = 524, Level = LogLevel.Error, Message = "Error exporting to DICOM destination. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void DimseExportErrorWithRetry(this ILogger logger, TimeSpan timespan, int retryCount, Exception ex);

        [LoggerMessage(EventId = 525, Level = LogLevel.Information, Message = "Sending job to {aeTitle}@{hostIp}:{port}...")]
        public static partial void DimseExporting(this ILogger logger, string aeTitle, string hostIp, int port);

        [LoggerMessage(EventId = 526, Level = LogLevel.Information, Message = "Job sent to {aeTitle}.")]
        public static partial void DimseExportComplete(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 527, Level = LogLevel.Information, Message = "Instance sent successfully.")]
        public static partial void DimseExportInstanceComplete(this ILogger logger);

        [LoggerMessage(EventId = 528, Level = LogLevel.Error, Message = "Failed to export with error {status}.")]
        public static partial void DimseExportInstanceError(this ILogger logger, DicomStatus status);

        [LoggerMessage(EventId = 529, Level = LogLevel.Error, Message = "Error while adding DICOM C-STORE request: {message}")]
        public static partial void DimseExportErrorAddingInstance(this ILogger logger, string message, Exception ex);

        [LoggerMessage(EventId = 530, Level = LogLevel.Error, Message = "{message}")]
        public static partial void ExportException(this ILogger logger, string message, Exception ex);

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

        // HTTP APIs

        // MONAI AE Title Controller
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
        [LoggerMessage(EventId = 810, Level = LogLevel.Information, Message = "DICOM destination added AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void DestinationApplicationEntityAdded(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 811, Level = LogLevel.Information, Message = "MONAI SCP Application Entity deleted {name}.")]
        public static partial void DestinationApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 812, Level = LogLevel.Error, Message = "Error querying DICOM destinations.")]
        public static partial void ErrorListingDestinationApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 813, Level = LogLevel.Error, Message = "Error adding new DICOM destination.")]
        public static partial void ErrorAddingDestinationApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 814, Level = LogLevel.Error, Message = "Error deleting DICOM destination.")]
        public static partial void ErrorDeletingDestinationApplicationEntity(this ILogger logger, Exception ex);

        // Source AE Title Controller
        [LoggerMessage(EventId = 820, Level = LogLevel.Information, Message = "DICOM source added AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void SourceApplicationEntityAdded(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 821, Level = LogLevel.Information, Message = "DICOM source deleted {name}.")]
        public static partial void SourceApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 822, Level = LogLevel.Error, Message = "Error querying DICOM sources.")]
        public static partial void ErrorListingSourceApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 823, Level = LogLevel.Error, Message = "Error adding new DICOM source.")]
        public static partial void ErrorAddingSourceApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 824, Level = LogLevel.Error, Message = "Error deleting DICOM source.")]
        public static partial void ErrorDeletingSourceApplicationEntity(this ILogger logger, Exception ex);

        // Inference API
        [LoggerMessage(EventId = 830, Level = LogLevel.Error, Message = "Failed to retrieve status for TransactionId/JobId={transactionId}.")]
        public static partial void ErrorRetrievingJobStatus(this ILogger logger, string transactionId, Exception ex);

        [LoggerMessage(EventId = 831, Level = LogLevel.Error, Message = "Failed to configure storage location for request: TransactionId={transactionId}.")]
        public static partial void ErrorConfiguringStorageLocation(this ILogger logger, string transactionId, Exception ex);

        [LoggerMessage(EventId = 832, Level = LogLevel.Error, Message = "Unable to queue the request: TransactionId={transactionId}.")]
        public static partial void ErrorQueuingInferenceRequest(this ILogger logger, string transactionId, Exception ex);

        // Health API
        [LoggerMessage(EventId = 840, Level = LogLevel.Error, Message = "Error collecting system status.")]
        public static partial void ErrorCollectingSystemStatus(this ILogger logger, Exception ex);

        // Middleware
        [LoggerMessage(EventId = 890, Level = LogLevel.Error, Message = "HTTP error in request {path}.")]
        public static partial void HttpRequestError(this ILogger logger, string path, Exception ex);

        // Inference Request Repository
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

        // Payload Assembler
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
        public static partial void DropEmptyBUcket(this ILogger logger, string key);

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

        // Storage
        [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Temporary Storage Path={path}.")]
        public static partial void TemporaryStoragePath(this ILogger logger, string path);

        [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Storage Size: {totalSize:N0}. Reserved: {reservedSpace:N0}.")]
        public static partial void StorageSizeWithReserve(this ILogger logger, long totalSize, long reservedSpace);

        [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "Storage Size: {totalSize:N0}. Reserved: {reservedSpace:N0}. Available: {freeSpace:N0}.")]
        public static partial void StorageSizeWithReserveAndAvailable(this ILogger logger, long totalSize, long reservedSpace, long freeSpace);

        [LoggerMessage(EventId = 4003, Level = LogLevel.Debug, Message = "Waiting for instance...")]
        public static partial void SpaceReclaimerWaitingForTask(this ILogger logger);

        [LoggerMessage(EventId = 4004, Level = LogLevel.Error, Message = "Error occurred deleting file {file} on {retryCount} retry.")]
        public static partial void ErrorDeletingFIle(this ILogger logger, string file, int retryCount, Exception ex);

        [LoggerMessage(EventId = 4005, Level = LogLevel.Debug, Message = "Deleting file {filePath}.")]
        public static partial void DeletingFile(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 4006, Level = LogLevel.Debug, Message = "File deleted {filePath}.")]
        public static partial void FileDeleted(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 4007, Level = LogLevel.Debug, Message = "Deleting directory {directory}.")]
        public static partial void DeletingDirectory(this ILogger logger, string directory);

        [LoggerMessage(EventId = 4008, Level = LogLevel.Error, Message = "Error deleting directory {directory}.")]
        public static partial void ErrorDeletingDirectory(this ILogger logger, string directory, Exception ex);

        [LoggerMessage(EventId = 4009, Level = LogLevel.Debug, Message = "File added to cleanup queue {file}. Queue size: {size}.")]
        public static partial void InstanceAddedToCleanupQueue(this ILogger logger, string file, int size);
    }
}
