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
            _logger.Log(LogLevel.Information, "Disk Space Reclaimer Hosted Service is running.");
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Log(LogLevel.Debug, "Waiting for instance...");
                try
                {
                    var file = _taskQueue.Dequeue(stoppingToken);

                    if (file is null) continue; // likely canceled

                    ProcessFile(file);
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
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.Log(LogLevel.Information, "Cancellation requested.");
        }

        private void ProcessFile(FileStorageInfo file)
        {
            Guard.Against.Null(file, nameof(file));

            Policy.Handle<Exception>()
                .WaitAndRetry(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, retryCount, context) =>
                    {
                        _logger.Log(LogLevel.Error, exception, $"Error occurred deleting file {file} on {retryCount} retry.");
                    })
                .Execute(() =>
                {
                    foreach (var filePath in file.FilePaths)
                    {
                        _logger.Log(LogLevel.Debug, $"Deleting file {filePath}");
                        if (_fileSystem.File.Exists(filePath))
                        {
                            _fileSystem.File.Delete(filePath);
                            _logger.Log(LogLevel.Debug, $"File deleted {filePath}");
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
                    _logger.Log(LogLevel.Debug, $"Deleting directory {dirPath}");
                    _fileSystem.Directory.Delete(dirPath);
                    RecursivelyRemoveDirectoriesIfEmpty(_fileSystem.Directory.GetParent(dirPath).FullName);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex, $"Error deleting directory {dirPath}.");
                }
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
