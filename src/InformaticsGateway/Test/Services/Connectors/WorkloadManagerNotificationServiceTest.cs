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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class WorkloadManagerNotificationServiceTest
    {
        private readonly Mock<ILogger<WorkloadManagerNotificationService>> _logger;
        private readonly Mock<IWorkloadManagerApi> _workloadManagerApi;
        private readonly Mock<IFileStoredNotificationQueue> _taskQueue;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IInstanceCleanupQueue> _cleanupQueue;

        public WorkloadManagerNotificationServiceTest()
        {
            _logger = new Mock<ILogger<WorkloadManagerNotificationService>>();
            _workloadManagerApi = new Mock<IWorkloadManagerApi>();
            _taskQueue = new Mock<IFileStoredNotificationQueue>();
            _fileSystem = new Mock<IFileSystem>();
            _cleanupQueue = new Mock<IInstanceCleanupQueue>();
        }

        [RetryFact(5, 250, DisplayName = "Shall start/stop service")]
        public async Task ShallStartStopService()
        {
            var service = new WorkloadManagerNotificationService(
                _workloadManagerApi.Object,
                _taskQueue.Object,
                _logger.Object,
                _fileSystem.Object,
                _cleanupQueue.Object);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Running, service.Status);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Stopped, service.Status);
        }

        [RetryFact(5, 250, DisplayName = "Shall upload file and queue for delete")]
        public async Task ShallUploadFileAndQueueForDelete()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var files = new List<FileStorageInfo> {
                new FileStorageInfo{  FilePath = "/dir1/file1"},
                new FileStorageInfo{  FilePath = "/dir1/file2"},
                new FileStorageInfo{  FilePath = "/dir2/file3"},
                new FileStorageInfo{  FilePath = "/dir3/file4"},
                new FileStorageInfo{  FilePath = "/dir4/file5"},
            };
            var stack = new Stack<FileStorageInfo>(files);
            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(true);
            _workloadManagerApi.Setup(p => p.Upload(It.IsAny<FileStorageInfo>(), It.IsAny<CancellationToken>()));
            _cleanupQueue.Setup(p => p.Queue(It.IsAny<FileStorageInfo>()));
            _taskQueue.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (stack.Count > 0)
                    {
                        return stack.Pop();
                    }
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException();
                });

            var service = new WorkloadManagerNotificationService(
                _workloadManagerApi.Object,
                _taskQueue.Object,
                _logger.Object,
                _fileSystem.Object,
                _cleanupQueue.Object);

            await service.StartAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Running, service.Status);

            WaitHandle.WaitAll(new[] { cancellationTokenSource.Token.WaitHandle }, 3000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Stopped, service.Status);

            _fileSystem.Verify(p => p.File.Exists(It.IsAny<string>()), Times.Exactly(files.Count));
            _workloadManagerApi.Verify(p => p.Upload(It.IsAny<FileStorageInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(files.Count));
            _cleanupQueue.Verify(p => p.Queue(It.IsAny<FileStorageInfo>()), Times.Exactly(files.Count));

            foreach (var file in files)
            {
                _logger.VerifyLogging($"Uploaded {file.FilePath} to Workload Manager.", LogLevel.Information, Times.Once());
            }
        }

        [RetryFact(5, 250, DisplayName = "Shall requeue file upon upload failure")]
        public async Task ShallRequeueFileUponUploadFailure()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var files = new List<FileStorageInfo> {
                new FileStorageInfo{  FilePath = "/dir1/file1"},
                new FileStorageInfo{  FilePath = "/dir1/file2"},
                new FileStorageInfo{  FilePath = "/dir2/file3"},
                new FileStorageInfo{  FilePath = "/dir3/file4"},
                new FileStorageInfo{  FilePath = "/dir4/file5"},
            };
            var stack = new Stack<FileStorageInfo>(files);
            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(true);
            _workloadManagerApi.Setup(p => p.Upload(It.IsAny<FileStorageInfo>(), It.IsAny<CancellationToken>())).Throws(new Exception());
            _cleanupQueue.Setup(p => p.Queue(It.IsAny<FileStorageInfo>()));
            _taskQueue.Setup(p => p.Queue(It.IsAny<FileStorageInfo>()));
            _taskQueue.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (stack.Count > 0)
                    {
                        return stack.Pop();
                    }
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException();
                });

            var service = new WorkloadManagerNotificationService(
                _workloadManagerApi.Object,
                _taskQueue.Object,
                _logger.Object,
                _fileSystem.Object,
                _cleanupQueue.Object);

            await service.StartAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Running, service.Status);

            WaitHandle.WaitAll(new[] { cancellationTokenSource.Token.WaitHandle }, 3000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Stopped, service.Status);

            _fileSystem.Verify(p => p.File.Exists(It.IsAny<string>()), Times.Exactly(files.Count));
            _workloadManagerApi.Verify(p => p.Upload(It.IsAny<FileStorageInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(files.Count));
            _cleanupQueue.Verify(p => p.Queue(It.IsAny<FileStorageInfo>()), Times.Never());
            _taskQueue.Verify(p => p.Queue(It.IsAny<FileStorageInfo>()), Times.Exactly(files.Count));

            foreach (var file in files)
            {
                _logger.VerifyLogging($"Error uploading file {file.FilePath} to Workload Manager.", LogLevel.Error, Times.Once());
            }
        }

        [RetryFact(5, 250, DisplayName = "Shall queue for clean if file is missing")]
        public async Task ShallQueueForDeleteIfFileIsMissing()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var files = new List<FileStorageInfo> {
                new FileStorageInfo{  FilePath = "/dir1/file1"},
                new FileStorageInfo{  FilePath = "/dir1/file2"},
                new FileStorageInfo{  FilePath = "/dir2/file3"},
                new FileStorageInfo{  FilePath = "/dir3/file4"},
                new FileStorageInfo{  FilePath = "/dir4/file5"},
            };
            var stack = new Stack<FileStorageInfo>(files);
            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(false);
            _workloadManagerApi.Setup(p => p.Upload(It.IsAny<FileStorageInfo>(), It.IsAny<CancellationToken>()));
            _cleanupQueue.Setup(p => p.Queue(It.IsAny<FileStorageInfo>()));
            _taskQueue.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (stack.Count > 0)
                    {
                        return stack.Pop();
                    }
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException();
                });

            var service = new WorkloadManagerNotificationService(
                _workloadManagerApi.Object,
                _taskQueue.Object,
                _logger.Object,
                _fileSystem.Object,
                _cleanupQueue.Object);

            await service.StartAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Running, service.Status);

            WaitHandle.WaitAll(new[] { cancellationTokenSource.Token.WaitHandle }, 3000);

            await service.StopAsync(cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Stopped, service.Status);

            _fileSystem.Verify(p => p.File.Exists(It.IsAny<string>()), Times.Exactly(files.Count));
            _workloadManagerApi.Verify(p => p.Upload(It.IsAny<FileStorageInfo>(), It.IsAny<CancellationToken>()), Times.Never());
            _cleanupQueue.Verify(p => p.Queue(It.IsAny<FileStorageInfo>()), Times.Exactly(files.Count));

            foreach (var file in files)
            {
                _logger.VerifyLogging($"Unable to upload file {file.FilePath}; file may have been deleted.", LogLevel.Warning, Times.Once());
            }
        }
    }
}
