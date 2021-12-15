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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Repositories;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    /// <summary>
    /// An in-memory queue for providing any files/DICOM instances received by the Informatics Gateway to
    /// other internal services.
    /// </summary>
    public sealed class FileStoredNotificationQueue : IFileStoredNotificationQueue
    {
        private readonly BlockingCollection<FileStorageInfo> _workItems;
        private readonly ILogger<FileStoredNotificationQueue> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FileStoredNotificationQueue(
            ILogger<FileStoredNotificationQueue> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _workItems = new BlockingCollection<FileStorageInfo>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            LoadExistingStoredFilesFromDatabase();
        }

        private void LoadExistingStoredFilesFromDatabase()
        {
            var repository = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<FileStorageInfo>>();
            foreach (var item in repository.AsQueryable())
            {
                _logger.Log(LogLevel.Debug, "Adding existing file to queue: {0}", item.FilePath);
                _workItems.Add(item);
            }
        }

        /// <summary>
        /// Queues a new instance of FileStorageInfo.
        /// </summary>
        /// <param name="file">Instance to be queued</param>
        public async Task Queue(FileStorageInfo file)
        {
            Guard.Against.Null(file, nameof(file));
            var repository = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<FileStorageInfo>>();

            await repository.AddAsync(file);
            await repository.SaveChangesAsync();

            _workItems.Add(file);
            _logger.Log(LogLevel.Debug, "File added to cleanup queue {0}. Queue size: {1}", file.FilePath, _workItems.Count);
        }

        /// <summary>
        /// Dequeued an instance if available; otherwise, the call is blocked until an instance is available
        /// or when cancellation token is set.
        /// </summary>
        /// <param name="cancellationToken">Instance of cancellation token</param>
        /// <returns>Instance of FileStorageInfo</returns>
        public async Task<FileStorageInfo> Dequeue(CancellationToken cancellationToken)
        {
            var item = _workItems.Take(cancellationToken);
            var repository = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<FileStorageInfo>>();
            try
            {
                repository.Remove(item);
                await repository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.Log(LogLevel.Warning, $"Error deleting {item.FilePath} from database; conflict detected.");
            }
            return item;
        }
    }
}
