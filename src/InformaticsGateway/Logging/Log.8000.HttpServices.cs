/*
 * Copyright 2023 MONAI Consortium
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
        // HTTP APIs

        // MONAI AE Title Controller
        [LoggerMessage(EventId = 8000, Level = LogLevel.Error, Message = "Error querying MONAI Application Entity.")]
        public static partial void ErrorListingMonaiApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8001, Level = LogLevel.Error, Message = "Error adding MONAI Application Entity.")]
        public static partial void ErrorAddingMonaiApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8002, Level = LogLevel.Error, Message = "Error deleting MONAI Application Entity.")]
        public static partial void ErrorDeletingMonaiApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8003, Level = LogLevel.Information, Message = "MONAI SCP AE Title added AE Title={aeTitle}.")]
        public static partial void MonaiApplicationEntityAdded(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 8004, Level = LogLevel.Information, Message = "MONAI SCP Application Entity deleted {name}.")]
        public static partial void MonaiApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 8005, Level = LogLevel.Information, Message = "MONAI SCP AE Title {name} updated AE Title={aeTitle}.")]
        public static partial void MonaiApplicationEntityUpdated(this ILogger logger, string name, string aeTitle);

        [LoggerMessage(EventId = 8006, Level = LogLevel.Error, Message = "Error reading data input plug-ins.")]
        public static partial void ErrorReadingDataInputPlugIns(this ILogger logger, Exception ex);

        // Destination AE Title Controller
        [LoggerMessage(EventId = 8010, Level = LogLevel.Information, Message = "DICOM destination added AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void DestinationApplicationEntityAdded(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 8011, Level = LogLevel.Information, Message = "DICOM destination deleted {name}.")]
        public static partial void DestinationApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 8012, Level = LogLevel.Error, Message = "Error querying DICOM destinations.")]
        public static partial void ErrorListingDestinationApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8013, Level = LogLevel.Error, Message = "Error adding new DICOM destination.")]
        public static partial void ErrorAddingDestinationApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8014, Level = LogLevel.Error, Message = "Error deleting DICOM destination.")]
        public static partial void ErrorDeletingDestinationApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8015, Level = LogLevel.Error, Message = "Error C-ECHO to DICOM destination {name}.")]
        public static partial void ErrorCEechoDestinationApplicationEntity(this ILogger logger, string name, Exception ex);

        [LoggerMessage(EventId = 8016, Level = LogLevel.Information, Message = "DICOM destination updated {name}: AE Title={aeTitle}, Host/IP={hostIp}, Port={port}.")]
        public static partial void DestinationApplicationEntityUpdated(this ILogger logger, string name, string aeTitle, string hostIp, int port);

        // Source AE Title Controller
        [LoggerMessage(EventId = 8020, Level = LogLevel.Information, Message = "DICOM source added AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void SourceApplicationEntityAdded(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 8021, Level = LogLevel.Information, Message = "DICOM source deleted {name}.")]
        public static partial void SourceApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 8022, Level = LogLevel.Error, Message = "Error querying DICOM sources.")]
        public static partial void ErrorListingSourceApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8023, Level = LogLevel.Error, Message = "Error adding new DICOM source.")]
        public static partial void ErrorAddingSourceApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8024, Level = LogLevel.Error, Message = "Error deleting DICOM source.")]
        public static partial void ErrorDeletingSourceApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8025, Level = LogLevel.Information, Message = "DICOM source updated {name}: AE Title={aeTitle}, Host/IP={hostIp}.")]
        public static partial void SourceApplicationEntityUpdated(this ILogger logger, string name, string aeTitle, string hostIp);

        // Inference API
        [LoggerMessage(EventId = 8030, Level = LogLevel.Error, Message = "Failed to retrieve status for TransactionId/JobId={transactionId}.")]
        public static partial void ErrorRetrievingJobStatus(this ILogger logger, string transactionId, Exception ex);

        [LoggerMessage(EventId = 8031, Level = LogLevel.Error, Message = "Failed to configure storage location for request: TransactionId={transactionId}.")]
        public static partial void ErrorConfiguringStorageLocation(this ILogger logger, string transactionId, Exception ex);

        [LoggerMessage(EventId = 8032, Level = LogLevel.Error, Message = "Unable to queue the request: TransactionId={transactionId}.")]
        public static partial void ErrorQueuingInferenceRequest(this ILogger logger, string transactionId, Exception ex);

        // Health API
        [LoggerMessage(EventId = 8040, Level = LogLevel.Error, Message = "Error collecting system status.")]
        public static partial void ErrorCollectingSystemStatus(this ILogger logger, Exception ex);

        // Virtual AE Title Controller
        [LoggerMessage(EventId = 8050, Level = LogLevel.Error, Message = "Error querying Virtual Application Entity.")]
        public static partial void ErrorListingVirtualApplicationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8051, Level = LogLevel.Error, Message = "Error adding Virtual Application Entity.")]
        public static partial void ErrorAddingVirtualApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8052, Level = LogLevel.Error, Message = "Error deleting Virtual Application Entity.")]
        public static partial void ErrorDeletingVirtualApplicationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8053, Level = LogLevel.Information, Message = "Virtual SCP AE Title added AE Title={aeTitle}.")]
        public static partial void VirtualApplicationEntityAdded(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 8054, Level = LogLevel.Information, Message = "MONAI SCP Application Entity deleted {name}.")]
        public static partial void VirtualApplicationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 8055, Level = LogLevel.Information, Message = "Virtual SCP AE Title {name} updated AE Title={aeTitle}.")]
        public static partial void VirtualApplicationEntityUpdated(this ILogger logger, string name, string aeTitle);

        // Middleware
        [LoggerMessage(EventId = 8090, Level = LogLevel.Error, Message = "HTTP error in request {path}.")]
        public static partial void HttpRequestError(this ILogger logger, string path, Exception ex);

        // DicomWeb STOW
        [LoggerMessage(EventId = 8100, Level = LogLevel.Error, Message = "Error processing DICOMWeb STOW StudyInstanceUID= {studyInstanceUid}, Workflow={workflowName}.")]
        public static partial void ErrorDicomWebStow(this ILogger logger, string? studyInstanceUid, string? workflowName, Exception? ex);

        [LoggerMessage(EventId = 8101, Level = LogLevel.Error, Message = "Error processing DICOMWeb STOW StudyInstanceUID is invalid '{studyInstanceUid}'.")]
        public static partial void ErrorDicomWebStowInvalidStudyInstanceUid(this ILogger logger, string? studyInstanceUid, Exception? ex);

        [LoggerMessage(EventId = 8102, Level = LogLevel.Warning, Message = "The parameter '{parameterName} with the value of '{parameterValue}' in the multipart message is ignored.")]
        public static partial void MultipartParameterIgnored(this ILogger logger, string parameterName, string parameterValue);

        [LoggerMessage(EventId = 8103, Level = LogLevel.Debug, Message = "Converting stream to FileBufferingReadStream with memory threashold={memoryThreashold} using temp directory={tempPath}.")]
        public static partial void ConvertingStreamToFileBufferingReadStream(this ILogger logger, int memoryThreashold, string tempPath);

        [LoggerMessage(EventId = 8104, Level = LogLevel.Error, Message = "Error saving instance from STOW service stream.")]
        public static partial void FailedToSaveInstance(this ILogger logger, Exception? ex);

        [LoggerMessage(EventId = 8105, Level = LogLevel.Error, Message = "Failed to open STOW service stream.")]
        public static partial void FailedToOpenStream(this ILogger logger, Exception? ex);

        [LoggerMessage(EventId = 8106, Level = LogLevel.Error, Message = "Failed to process STOW request, out of storage space.")]
        public static partial void StowFailedWithNoSpace(this ILogger logger, Exception? ex = null);

        [LoggerMessage(EventId = 8108, Level = LogLevel.Information, Message = "STOW instance queued.")]
        public static partial void QueuedStowInstance(this ILogger logger);

        [LoggerMessage(EventId = 8109, Level = LogLevel.Information, Message = "Saving {count} DICOMWeb STOW-RS streams.")]
        public static partial void SavingStream(this ILogger logger, int count);

        [LoggerMessage(EventId = 8110, Level = LogLevel.Warning, Message = "Ignoring zero length stream.")]
        public static partial void ZeroLengthDicomWebStowStream(this ILogger logger);

        [LoggerMessage(EventId = 8111, Level = LogLevel.Error, Message = "Unknown virtual application entity specified {aet}.")]
        public static partial void ErrorDicomWebStowUnknownVirtualApplicationEntity(this ILogger logger, string? aet, Exception ex);

        // FHIR Serer

        [LoggerMessage(EventId = 8200, Level = LogLevel.Debug, Message = "Parsing FHIR as JSON.")]
        public static partial void ParsingFhirJson(this ILogger logger);

        [LoggerMessage(EventId = 8201, Level = LogLevel.Debug, Message = "Parsing FHIR as XML.")]
        public static partial void ParsingFhirXml(this ILogger logger);

        [LoggerMessage(EventId = 8202, Level = LogLevel.Information, Message = "FHIR instance queued.")]
        public static partial void QueueFhirInstance(this ILogger logger);

        [LoggerMessage(EventId = 8203, Level = LogLevel.Error, Message = "Error storing FHIR object.")]
        public static partial void FhirStoreException(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8204, Level = LogLevel.Error, Message = "Failed to store FHIR resource.")]
        public static partial void ErrorStoringFhirResource(this ILogger logger, Exception ex);

        //
        // Dicom Associations Controller.
        //
        [LoggerMessage(EventId = 8300, Level = LogLevel.Error, Message = "Unexpected error occurred in GET /dicom-associations API..")]
        public static partial void DicomAssociationsControllerGetError(this ILogger logger, Exception ex);

        ///
        /// HL7 Application Configuration controller
        ///
        [LoggerMessage(EventId = 8400, Level = LogLevel.Error, Message = "Unexpected error occurred in PUT {endpoint} API.")]
        public static partial void PutHl7ApplicationConfigException(this ILogger logger, string endpoint, Exception ex);


        // HL7 Destination Controller
        [LoggerMessage(EventId = 8401, Level = LogLevel.Information, Message = "HL7 destination added Name={name}, Host/IP={hostIp}.")]
        public static partial void HL7DestinationEntityAdded(this ILogger logger, string name, string hostIp);

        [LoggerMessage(EventId = 8402, Level = LogLevel.Information, Message = "HL7 destination deleted {name}.")]
        public static partial void HL7DestinationEntityDeleted(this ILogger logger, string name);

        [LoggerMessage(EventId = 8403, Level = LogLevel.Error, Message = "Error querying HL7 destinations.")]
        public static partial void ErrorListingHL7DestinationEntities(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8404, Level = LogLevel.Error, Message = "Error adding new HL7 destination.")]
        public static partial void ErrorAddingHL7DestinationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8405, Level = LogLevel.Error, Message = "Error deleting HL7 destination.")]
        public static partial void ErrorDeletingHL7DestinationEntity(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 8406, Level = LogLevel.Error, Message = "Error C-ECHO to HL7 destination {name}.")]
        public static partial void ErrorCEechoHL7DestinationEntity(this ILogger logger, string name, Exception ex);

        [LoggerMessage(EventId = 8407, Level = LogLevel.Information, Message = "HL7 destination updated {name}: Host/IP={hostIp}, Port={port}.")]
        public static partial void HL7DestinationEntityUpdated(this ILogger logger, string name, string hostIp, int port);
    }
}
