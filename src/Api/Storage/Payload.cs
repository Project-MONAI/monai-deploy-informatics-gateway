/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ardalis.GuardClauses;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public class Payload : IDisposable
    {
        public enum PayloadState
        {
            /// <summary>
            /// Payload created and waiting for files to be added.
            /// </summary>
            Created,

            /// <summary>
            /// Files in the payload have been uploaded, assembled and ready to be moved into the payload directory.
            /// </summary>
            Move,

            /// <summary>
            /// Payload is ready to be published to the message broker.
            /// </summary>
            Notify
        }

        public const int MAX_RETRY = 3;
        private readonly Stopwatch _lastReceived;
        private bool _disposedValue;

        public Guid PayloadId { get; private set; }

        public uint Timeout { get; init; }

        public string Key { get; init; }

        public string? MachineName { get; init; }

        public DateTime DateTimeCreated { get; private set; }

        public int RetryCount { get; set; }

        public PayloadState State { get; set; }

        public List<FileStorageMetadata> Files { get; init; }

        public string CorrelationId { get; init; }

        public string? WorkflowInstanceId { get; init; }

        public string? TaskId { get; init; }

        public DataOrigin DataTrigger { get; init; }

        public HashSet<DataOrigin> DataOrigins { get; init; }

        public int Count { get => Files.Count; }

        public bool HasTimedOut { get => ElapsedTime().TotalSeconds >= Timeout; }

        public TimeSpan Elapsed
        {
            get { return DateTime.UtcNow.Subtract(DateTimeCreated); }
        }

        public int FilesUploaded { get => Files.Count(p => p.IsUploaded); }

        public int FilesFailedToUpload { get => Files.Count(p => p.IsUploadFailed); }

        public Payload() { }

        public Payload(string key, string correlationId, string? workflowInstanceId, string? taskId, DataOrigin dataTrigger, uint timeout)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));
            Files = new List<FileStorageMetadata>();
            DataOrigins = new HashSet<DataOrigin>();
            _lastReceived = new Stopwatch();

            CorrelationId = correlationId;
            WorkflowInstanceId = workflowInstanceId;
            TaskId = taskId;
            MachineName = Environment.MachineName;
            DateTimeCreated = DateTime.UtcNow;
            PayloadId = Guid.NewGuid();
            Key = key;
            State = PayloadState.Created;
            RetryCount = 0;
            Timeout = timeout;
            DataTrigger = dataTrigger;
        }

        public Payload(string key, string correlationId, string? workflowInstanceId, string? taskId, DataOrigin dataTrigger, uint timeout, string? payloadId) :
            this(key, correlationId, workflowInstanceId, taskId, dataTrigger, timeout)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));

            if (payloadId is null)
            {
                PayloadId = Guid.NewGuid();
            }
            else
            {
                PayloadId = Guid.Parse(payloadId);
            }
        }

        public void Add(FileStorageMetadata value)
        {
            Guard.Against.Null(value, nameof(value));

            Files.Add(value);

            if (!DataTrigger.Equals(value.DataOrigin))
            {
                DataOrigins.Add(value.DataOrigin);
            }

            //if (string.IsNullOrWhiteSpace(value.DestinationFolderNeil) is false)
            //{
            //    DestinationFolder = value.DestinationFolderNeil;
            //}

            _lastReceived.Reset();
            _lastReceived.Start();
        }

        public TimeSpan ElapsedTime()
        {
            return _lastReceived.Elapsed;
        }

        public void ResetRetry()
        {
            RetryCount = 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _lastReceived.Stop();
                    Files.Clear();
                }

                _disposedValue = true;
            }
        }

        public override string ToString()
        {
            return $"PayloadId: {PayloadId}/Key: {Key}";
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
