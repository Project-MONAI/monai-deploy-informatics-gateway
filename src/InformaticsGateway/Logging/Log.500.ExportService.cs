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
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 500, Level = LogLevel.Information, Message = "{ServiceName} subscribed to {RoutingKey} messages.")]
        public static partial void ExportEventSubscription(this ILogger logger, string serviceName, string routingKey);

        [LoggerMessage(EventId = 501, Level = LogLevel.Warning, Message = "{ServiceName} paused due to insufficient storage space.  Available storage space: {availableFreeSpace:D}.")]
        public static partial void ExportPausedDueToInsufficientStorageSpace(this ILogger logger, string serviceName, long availableFreeSpace);

        [LoggerMessage(EventId = 502, Level = LogLevel.Warning, Message = "CorrelationId={correlationId}. The export request {exportTaskId} is already queued for export.")]
        public static partial void ExportRequestAlreadyQueued(this ILogger logger, string correlationId, string exportTaskId);

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

        [LoggerMessage(EventId = 509, Level = LogLevel.Information, Message = "Sending acknowledgment.")]
        public static partial void SendingAcknowledgement(this ILogger logger);

        [LoggerMessage(EventId = 510, Level = LogLevel.Error, Message = "Error publishing message. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.")]
        public static partial void ErrorPublishingExportCompleteEventWithRetry(this ILogger logger, Exception ex, TimeSpan timeSpan, int retryCount);

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
        public static partial void ExportAssociationAccepted(this ILogger logger);

        [LoggerMessage(EventId = 522, Level = LogLevel.Warning, Message = "Association rejected.")]
        public static partial void ExportAssociationRejected(this ILogger logger);

        [LoggerMessage(EventId = 523, Level = LogLevel.Information, Message = "Association released.")]
        public static partial void ExportAssociationReleased(this ILogger logger);

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

        [LoggerMessage(EventId = 529, Level = LogLevel.Error, Message = "{message}")]
        public static partial void DimseExportErrorAddingInstance(this ILogger logger, string message, Exception ex);

        [LoggerMessage(EventId = 530, Level = LogLevel.Error, Message = "{message}")]
        public static partial void ExportException(this ILogger logger, string message, Exception ex);

        [LoggerMessage(EventId = 531, Level = LogLevel.Warning, Message = "Export service paused due to insufficient storage space.  Available storage space: {availableFreeSpace:D}")]
        public static partial void ExportServiceStoppedDueToLowStorageSpace(this ILogger logger, long availableFreeSpace);

        [LoggerMessage(EventId = 532, Level = LogLevel.Information, Message = "CorrelationId={correlationId}. Export request {exportTaskId} received & queued for processing.")]
        public static partial void ExportRequestQueuedForProcessing(this ILogger logger, string correlationId, string exportTaskId);
    }
}
