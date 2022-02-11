// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            /// Payload is ready or in the uplaod stage.
            /// </summary>
            Upload,

            /// <summary>
            /// Payload is ready to be published for processing.
            /// </summary>
            Notify
        }

        public const int MAX_RETRY = 3;

        private readonly Guid _id;
        private readonly Stopwatch _lastReceived;
        private int _fileCount;

        public Guid Id => _id;

        public uint Timeout { get; init; }

        public string Key { get; init; }

        public DateTime DateTimeCreated { get; private set; }

        public int RetryCount { get; set; }

        public bool HasTimedOut { get => ElapsedTime().TotalSeconds >= Timeout; }

        public PayloadState State { get; set; }

        public IList<FileStorageInfo> Files { get; }

        public int Count { get => _fileCount; }

        public ISet<string> Workflows { get; private set; }

        public string CorrelationId { get; init; }

        public IList<BlockStorageInfo> UploadedFiles { get; set; }

        public Payload(string key, string correlationId, uint timeout)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));

            _id = Guid.NewGuid();
            _lastReceived = new Stopwatch();
            _fileCount = 0;
            Key = key;
            CorrelationId = correlationId;
            Timeout = timeout;
            RetryCount = 0;
            State = PayloadState.Created;
            Files = new List<FileStorageInfo>();
            UploadedFiles = new List<BlockStorageInfo>();
            Workflows = new HashSet<string>();
        }

        public void Add(FileStorageInfo value)
        {
            Guard.Against.Null(value, nameof(value));

            Files.Add(value);
            _lastReceived.Reset();
            _lastReceived.Start();
            _fileCount = Files.Count;

            if (!value.Workflows.IsNullOrEmpty())
            {
                foreach (var workflow in value.Workflows)
                {

                    Workflows.Add(workflow);
                }
            }

            if (Files.Count == 1)
            {
                DateTimeCreated = value.Received;
            }
        }

        public TimeSpan ElapsedTime()
        {
            return _lastReceived.Elapsed;
        }

        public bool CanRetry()
        {
            return ++RetryCount < MAX_RETRY;
        }

        public void ResetRetry()
        {
            RetryCount = 0;
        }

        public void Dispose()
        {
            _lastReceived.Stop();
            Files.Clear();
        }
    }
}
