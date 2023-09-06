using System;
using System.Collections.Generic;
using System.Linq;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.Messaging.Events;
using static Monai.Deploy.InformaticsGateway.Api.Storage.Payload;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    public class PayloadDTO
    {
        public PayloadDTO(Payload payload)
        {
            PayloadId = payload.PayloadId;
            Key = payload.Key;
            MachineName = payload.MachineName;
            DateTimeCreated = payload.DateTimeCreated;
            State = payload.State;
            Files = payload.Files.Select(f => new FileInfo(f)).ToList();
            CorrelationId = payload.CorrelationId;
            WorkflowInstanceId = payload.WorkflowInstanceId;
            TaskId = payload.TaskId;
            DataTrigger = payload.DataTrigger;
            Count = payload.Count;
            FilesUploaded = payload.FilesUploaded;
            FilesFailedToUpload = payload.FilesFailedToUpload;
        }

        public Guid PayloadId { get; private set; }

        public uint Timeout { get; init; }

        public string Key { get; init; }

        public string? MachineName { get; init; }

        public DateTime DateTimeCreated { get; private set; }

        public PayloadState State { get; set; }

        public List<FileInfo> Files { get; init; }

        public string CorrelationId { get; init; }

        public string? WorkflowInstanceId { get; init; }

        public string? TaskId { get; init; }

        public DataOrigin DataTrigger { get; init; }

        public HashSet<DataOrigin> DataOrigins { get; init; }

        public int Count;

        public bool HasTimedOut;

        public int FilesUploaded;

        public int FilesFailedToUpload;
    }

    public class FileInfo
    {
        public FileInfo(FileStorageMetadata meta)
        {
            FileName = meta.File.UploadPath;
            FileStatus = "Pending";
            if (meta.File.IsMoveCompleted)
            {
                FileStatus = "Complete";
            }
            if (meta.File.IsUploadFailed)
            {
                FileStatus = "Failed";
            }
        }

        public string FileName { get; set; }

        public string FileStatus { get; set; }
    }
}
