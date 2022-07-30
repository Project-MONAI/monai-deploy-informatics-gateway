/*
 * Copyright 2022 MONAI Consortium
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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class ObjectUploadQueue : IObjectUploadQueue
    {
        private readonly BlockingCollection<FileStorageMetadata> _workItems;
        private readonly ILogger<ObjectUploadQueue> _logger;

        public ObjectUploadQueue(ILogger<ObjectUploadQueue> logger)
        {
            _workItems = new BlockingCollection<FileStorageMetadata>();
            _logger = logger;
        }

        public void Queue(FileStorageMetadata file)
        {
            Guard.Against.Null(file, nameof(file));
            _workItems.Add(file);

            var process = Process.GetCurrentProcess();

            _logger.InstanceAddedToUploadQueue(file.Id, _workItems.Count, process.WorkingSet64 / 1024.0);
        }

        public FileStorageMetadata Dequeue(CancellationToken cancellationToken)
        {
            return _workItems.Take(cancellationToken);
        }
    }
}
