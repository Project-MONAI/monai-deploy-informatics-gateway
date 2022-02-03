// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2020 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using System.Collections.Concurrent;
using System.Threading;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class InstanceCleanupQueue : IInstanceCleanupQueue
    {
        private readonly BlockingCollection<FileStorageInfo> _workItems;
        private readonly ILogger<InstanceCleanupQueue> _logger;

        public InstanceCleanupQueue(ILogger<InstanceCleanupQueue> logger)
        {
            _workItems = new BlockingCollection<FileStorageInfo>();
            _logger = logger;
        }

        public void Queue(FileStorageInfo file)
        {
            Guard.Against.Null(file, nameof(file));

            _workItems.Add(file);
            _logger.Log(LogLevel.Debug, "File added to cleanup queue {0}. Queue size: {1}", file, _workItems.Count);
        }

        public FileStorageInfo Dequeue(CancellationToken cancellationToken)
        {
            return _workItems.Take(cancellationToken);
        }
    }
}
