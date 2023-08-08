using System;
using System.Collections.Generic;
using FellowOakDicom;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public class RemoteAppExecution
    {
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public string ExportTaskId { get; set; } = string.Empty;
        public string WorkflowInstanceId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? StudyUid { get; set; }
        public string? OutgoingStudyUid { get; set; }
        public List<DestinationApplicationEntity> ExportDetails { get; set; } = new();
        public List<string> Files { get; set; } = new();
        public FileExportStatus Status { get; set; }
        public Dictionary<DicomTag, string> OriginalValues { get; set; } = new();
    }
}
