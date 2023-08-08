using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.ExecutionPlugins
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Changed the StudyUid from {OriginalStudyUid} to {NewStudyUid}")]
        public static partial void LogStudyUidChanged(this ILogger logger, string OriginalStudyUid, string NewStudyUid);

        [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Cannot find entry for OriginalStudyUid {OriginalStudyUid} ")]
        public static partial void LogOriginalStudyUidNotFound(this ILogger logger, string OriginalStudyUid);
    }
}
