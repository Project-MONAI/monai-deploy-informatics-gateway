// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;

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
        private readonly List<FileStorageInfo> _files;
        private bool _disposedValue;

        public Guid Id { get; }

        public uint Timeout { get; init; }

        public string Key { get; init; }

        public DateTime DateTimeCreated { get; private set; }

        public int RetryCount { get; set; }

        public PayloadState State { get; set; }

        [BackingField(nameof(_files))]
        public IReadOnlyList<FileStorageInfo> Files { get => _files; }

        public string CorrelationId { get; init; }

        public int Count { get => _files.Count; }

        public bool HasTimedOut { get => ElapsedTime().TotalSeconds >= Timeout; }

        public string CallingAeTitle { get => _files.OfType<DicomFileStorageInfo>().Select(p => p.CallingAeTitle).FirstOrDefault(); }

        public string CalledAeTitle { get => _files.OfType<DicomFileStorageInfo>().Select(p => p.CalledAeTitle).FirstOrDefault(); }

        public Payload(string key, string correlationId, uint timeout)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));

            _files = new List<FileStorageInfo>();
            _lastReceived = new Stopwatch();

            CorrelationId = correlationId;
            DateTimeCreated = DateTime.UtcNow;
            Id = Guid.NewGuid();
            Key = key;
            State = PayloadState.Created;
            RetryCount = 0;
            Timeout = timeout;
        }

        public void Add(FileStorageInfo value)
        {
            Guard.Against.Null(value, nameof(value));

            _files.Add(value);
            _lastReceived.Reset();
            _lastReceived.Start();
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
                    _files.Clear();
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
