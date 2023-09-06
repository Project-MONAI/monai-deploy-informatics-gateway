using System;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 6000, Level = LogLevel.Error, Message = "Failed to get DICOM tag {dicomTag} in bucket {bucketId}. Payload: {payloadId}")]
        public static partial void FailedToGetDicomTagFromPayload(this ILogger logger, string payloadId, string dicomTag, string bucketId, Exception ex);

        [LoggerMessage(EventId = 6001, Level = LogLevel.Information, Message = "Attempted to retrieve Patient Name from DCM file, result: {name}")]
        public static partial void GetPatientName(this ILogger logger, string name);

        [LoggerMessage(EventId = 6002, Level = LogLevel.Information, Message = "Unsupported Type '{vr}' {vrFull} with value: {value} result: '{result}'")]
        public static partial void UnsupportedType(this ILogger logger, string vr, string vrFull, string value, string result);

        [LoggerMessage(EventId = 6003, Level = LogLevel.Information, Message = "Decoding supported type '{vr}' {vrFull} with value: {value} result: '{result}'")]
        public static partial void SupportedType(this ILogger logger, string vr, string vrFull, string value, string result);

        [LoggerMessage(EventId = 6004, Level = LogLevel.Error, Message = "Failed trying to cast Dicom Value to string {value}")]
        public static partial void UnableToCastDicomValueToString(this ILogger logger, string value, Exception ex);

        [LoggerMessage(EventId = 6005, Level = LogLevel.Debug, Message = "Dicom export marked as succeeded with {fileStatusCount} files marked as exported.")]
        public static partial void DicomExportSucceeded(this ILogger logger, string fileStatusCount);

        [LoggerMessage(EventId = 6006, Level = LogLevel.Debug, Message = "Dicom export marked as failed with {fileStatusCount} files marked as exported.")]
        public static partial void DicomExportFailed(this ILogger logger, string fileStatusCount);
    }
}
