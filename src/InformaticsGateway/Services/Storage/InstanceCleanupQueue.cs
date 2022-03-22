// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Concurrent;
using System.Threading;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Logging;

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
            _logger.InstanceAddedToCleanupQueue(file.UploadFilePath, _workItems.Count);
        }

        public FileStorageInfo Dequeue(CancellationToken cancellationToken)
        {
            return _workItems.Take(cancellationToken);
        }
    }
}
