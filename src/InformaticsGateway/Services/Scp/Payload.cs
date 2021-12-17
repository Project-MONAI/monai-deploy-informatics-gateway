// Copyright 2021 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class Payload : List<FileStorageInfo>, IDisposable
    {
        public const int MAX_RETRY = 3;
        private Stopwatch _lastReceived;
        public uint Timeout { get; }
        public string Key { get; }
        public int RetryCount { get; set; }

        public bool HasTimedOut { get => ElapsedTime().TotalSeconds >= Timeout; }

        public Payload(string key, uint timeout)
        {
            Guard.Against.NullOrWhiteSpace(key, nameof(key));
            _lastReceived = new Stopwatch();
            Key = key;
            Timeout = timeout;
            RetryCount = 0;
        }

        public void AddInstance(FileStorageInfo value)
        {
            Guard.Against.Null(value, nameof(value));

            Add(value);
            _lastReceived.Reset();
            _lastReceived.Start();
        }

        public TimeSpan ElapsedTime()
        {
            return _lastReceived.Elapsed;
        }

        public bool IncrementAndRetry()
        {
            return ++RetryCount < MAX_RETRY;
        }

        public void Dispose()
        {
            _lastReceived.Stop();
            Clear();
        }
    }
}
