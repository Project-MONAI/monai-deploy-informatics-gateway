// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class SpaceReclaimerService : IHostedService, IMonaiService
    {
        private readonly ILogger<SpaceReclaimerService> _logger;
        private readonly IInstanceCleanupQueue _taskQueue;
        private readonly IFileSystem _fileSystem;
        private readonly string _payloadDirectory;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public string ServiceName => "Space Reclaimer Service";

        public SpaceReclaimerService(
            IInstanceCleanupQueue taskQueue,
            ILogger<SpaceReclaimerService> logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IFileSystem fileSystem)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _payloadDirectory = configuration.Value.Storage.TemporaryDataDirFullPath;
        }

        private void BackgroundProcessing(CancellationToken stoppingToken)
        {
            _logger.ServiceRunning(ServiceName);
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.SpaceReclaimerWaitingForTask();
                try
                {
                    var file = _taskQueue.Dequeue(stoppingToken);

                    if (file is null) continue; // likely canceled

                    ProcessFile(file);
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.ServiceDisposed(ServiceName, ex);
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException || ex is OperationCanceledException)
                    {
                        _logger.ServiceInvalidOrCancelled(ServiceName, ex);
                    }
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        private void ProcessFile(FileStorageInfo file)
        {
            Guard.Against.Null(file, nameof(file));

            Policy.Handle<Exception>()
                .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timespan, retryCount, context) =>
                    {
                        _logger.ErrorDeletingFIle(file.FilePath, retryCount, exception);
                    })
                .Execute(() =>
                {
                    foreach (var filePath in file.FilePaths)
                    {
                        _logger.DeletingFile(filePath);
                        if (_fileSystem.File.Exists(filePath))
                        {
                            _fileSystem.File.Delete(filePath);
                            _logger.FileDeleted(filePath);
                        }
                    }

                    try
                    {
                        RecursivelyRemoveDirectoriesIfEmpty(_fileSystem.Path.GetDirectoryName(file.FilePath));
                    }
                    catch (DirectoryNotFoundException)
                    {
                        //no op
                    }
                });
        }

        private void RecursivelyRemoveDirectoriesIfEmpty(string dirPath)
        {
            if (_payloadDirectory.Equals(dirPath, StringComparison.OrdinalIgnoreCase) ||
                !_fileSystem.Directory.Exists(dirPath))
            {
                return;
            }

            var filesInDir = _fileSystem.Directory.GetFiles(dirPath);
            var dirsInDir = _fileSystem.Directory.GetDirectories(dirPath);
            if (filesInDir.Length + dirsInDir.Length == 0)
            {
                try
                {
                    _logger.DeletingDirectory(dirPath);
                    _fileSystem.Directory.Delete(dirPath);
                    RecursivelyRemoveDirectoriesIfEmpty(_fileSystem.Directory.GetParent(dirPath).FullName);
                }
                catch (Exception ex)
                {
                    _logger.ErrorDeletingDirectory(dirPath, ex);
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                BackgroundProcessing(cancellationToken);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceRunning(ServiceName);
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }
    }
}
