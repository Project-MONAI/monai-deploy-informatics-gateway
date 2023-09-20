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
using System.Net;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 600, Level = LogLevel.Information, Message = "Processing input source '{inputInterface}' from {uri}.")]
        public static partial void ProcessingInputResource(this ILogger logger, InputInterfaceType inputInterface, string uri);

        [LoggerMessage(EventId = 601, Level = LogLevel.Warning, Message = "Specified input interface is not supported '{inputInterface}`.")]
        public static partial void UnsupportedInputInterface(this ILogger logger, InputInterfaceType inputInterface);

        [LoggerMessage(EventId = 602, Level = LogLevel.Debug, Message = "Reading previously retrieved files...")]
        public static partial void RestoringRetrievedFiles(this ILogger logger);

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

        [LoggerMessage(EventId = 617, Level = LogLevel.Warning, Message = "Data retrieval paused due to insufficient storage space.Available storage space: {space:D}.")]
        public static partial void DataRetrievalPaused(this ILogger logger, long space);

        [LoggerMessage(EventId = 618, Level = LogLevel.Information, Message = "Processing inference request.")]
        public static partial void ProcessingInferenceRequest(this ILogger logger);

        [LoggerMessage(EventId = 619, Level = LogLevel.Information, Message = "Inference request completed and ready for job submission.")]
        public static partial void InferenceRequestProcessed(this ILogger logger);

        [LoggerMessage(EventId = 620, Level = LogLevel.Error, Message = "Error processing request: TransactionId = {transactionId}.")]
        public static partial void ErrorProcessingInferenceRequest(this ILogger logger, string transactionId, Exception ex);

        [LoggerMessage(EventId = 621, Level = LogLevel.Warning, Message = "FHIR resource '{file}' already retrieved/stored.")]
        public static partial void FhireResourceAlreadyExists(this ILogger logger, string file);

        [LoggerMessage(EventId = 622, Level = LogLevel.Warning, Message = "FHIR resource {type}/{id} contains no data.")]
        public static partial void FhirResourceContainsNoData(this ILogger logger, string type, string id);

        [LoggerMessage(EventId = 623, Level = LogLevel.Warning, Message = "Data retrieval paused due to insufficient storage space.  Available storage space: {availableFreeSpace:D}.")]
        public static partial void DataRetrievalServiceStoppedDueToLowStorageSpace(this ILogger logger, long availableFreeSpace);
    }
}
