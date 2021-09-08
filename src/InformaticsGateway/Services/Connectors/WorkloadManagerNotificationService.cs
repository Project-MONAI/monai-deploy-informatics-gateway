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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    public class WorkloadManagerNotificationService : IHostedService, IMonaiService
    {
        private readonly ILogger<WorkloadManagerNotificationService> _logger;
        private readonly IWorkloadManagerApi _workloadManagerApi;
        private readonly IFileStoredNotificationQueue _taskQueue;
        private readonly IFileSystem _fileSystem;
        private readonly IInstanceCleanupQueue _cleanupQueue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public WorkloadManagerNotificationService(
            IWorkloadManagerApi workloadManagerApi,
            IFileStoredNotificationQueue taskQueue,
            ILogger<WorkloadManagerNotificationService> logger,
            IFileSystem fileSystem,
            IInstanceCleanupQueue cleanupQueue)
        {
            _workloadManagerApi = workloadManagerApi ?? throw new ArgumentNullException(nameof(workloadManagerApi));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _cleanupQueue = cleanupQueue ?? throw new ArgumentNullException(nameof(cleanupQueue));
        }

        private void BackgroundProcessing(CancellationToken stoppingToken)
        {
            _logger.Log(LogLevel.Information, "MONAI Workload Manager Notification Hosted Service is running.");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Log(LogLevel.Debug, "Waiting for instance...");
                FileStorageInfo file = null;
                try
                {
                    file = _taskQueue.Dequeue(stoppingToken);

                    if (file is null) continue; // likely canceled

                    ProcessFile(file, stoppingToken);
                    _cleanupQueue.Queue(file);
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.Log(LogLevel.Critical, ex, "The cleanup queue may have been disposed.");
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException || ex is OperationCanceledException)
                    {
                        _logger.Log(LogLevel.Error, ex, "The cleanup queue may have been modified out marked as completed.");
                    }
                    else
                    {
                        _logger.Log(LogLevel.Error, ex, ex.Message);
                    }

                    if (file is not null)
                    {
                        file.TryCount++;
                        _taskQueue.Queue(file);
                    }
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.Log(LogLevel.Information, "Cancellation requested.");
        }

        private void ProcessFile(Api.FileStorageInfo file, CancellationToken stoppingToken)
        {
            Guard.Against.Null(file, nameof(file));

            if (!_fileSystem.File.Exists(file.FilePath))
            {
                //TODO: encrypt log
                _logger.Log(LogLevel.Warning, "Unable to upload file {0}; file may have been deleted.", file.FilePath);
                return;
            }

            try
            {
                _logger.Log(LogLevel.Debug, "Uploading {0} to Workload Manager.", file.FilePath);
                _workloadManagerApi.Upload(file, stoppingToken);
                _logger.Log(LogLevel.Information, "Uploaded {0} to Workload Manager.", file.FilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file {file.FilePath} to Workload Manager.", ex);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                BackgroundProcessing(cancellationToken);
            });

            Status = ServiceStatus.Running;
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Disk Space Reclaimer Hosted Service is stopping.");
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }
    }
}
