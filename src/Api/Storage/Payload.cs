// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Common;

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
        private readonly Stopwatch _lastReceived;
        private bool _disposedValue;

        public Guid Id { get; }

        public uint Timeout { get; init; }

        public string Key { get; init; }

        public DateTime DateTimeCreated { get; private set; }

        public int RetryCount { get; set; }

        public bool HasTimedOut { get => ElapsedTime().TotalSeconds >= Timeout; }

        public PayloadState State { get; set; }

        public IList<FileStorageInfo> Files { get; }

        public int Count { get; private set; }

        public ISet<string> Workflows { get; private set; }

        public string CorrelationId { get; init; }

        public IList<BlockStorageInfo> UploadedFiles { get; set; }

        public Payload(string key, string correlationId, uint timeout)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));

            Id = Guid.NewGuid();
            _lastReceived = new Stopwatch();
            Count = 0;
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
            Count = Files.Count;

            if (!value.Workflows.IsNullOrEmpty())
            {
                foreach (var workflow in value.Workflows)
                {
                    Workflows.Add(workflow);
                }
            }

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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
