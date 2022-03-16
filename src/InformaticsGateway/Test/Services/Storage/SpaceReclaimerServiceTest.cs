// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Storage
{
    public class SpaceReclaimerServiceTest
    {
        private readonly Mock<ILogger<SpaceReclaimerService>> _logger;
        private readonly Mock<IInstanceCleanupQueue> _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IFileSystem _fileSystem;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly SpaceReclaimerService _service;

        public SpaceReclaimerServiceTest()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = new Mock<ILogger<SpaceReclaimerService>>();
            _queue = new Mock<IInstanceCleanupQueue>();
            _fileSystem = new MockFileSystem();

            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _configuration.Value.Storage.Temporary = "/payloads";
            _service = new SpaceReclaimerService(_queue.Object, _logger.Object, _configuration, _fileSystem);
        }

        [RetryFact(5, 250, DisplayName = "Shall honor cancellation request")]
        public async Task ShallHonorCancellationRequest()
        {
            _queue.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(default(FileStorageInfo));

            _cancellationTokenSource.Cancel();
            var task = Task.Run(async () =>
            {
                await Task.Delay(150);
                await _service.StopAsync(_cancellationTokenSource.Token);
            });
            await _service.StartAsync(_cancellationTokenSource.Token);
            task.Wait();

            _queue.Verify(p => p.Dequeue(It.IsAny<CancellationToken>()), Times.Never());
            _logger.VerifyLogging("Cancellation requested.", LogLevel.Information, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Shall delete files")]
        public async Task ShallDeleteFiles()
        {
            var files = new List<FileStorageInfo>() {
                new FileStorageInfo{  FilePath ="/payloads/dir1/file1"},
                new FileStorageInfo{  FilePath ="/payloads/dir1/file2"},
                new FileStorageInfo{  FilePath ="/payloads/dir1/file3.exe"},
            };

            foreach (var file in files)
            {
                _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(file.FilePath));
                _fileSystem.File.Create(file.FilePath);
            }

            var stack = new Stack<FileStorageInfo>(files);
            _queue.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (stack.TryPop(out FileStorageInfo result))
                        return result;

                    _cancellationTokenSource.Cancel();
                    return null;
                });

            await _service.StartAsync(_cancellationTokenSource.Token);
            while (!_cancellationTokenSource.IsCancellationRequested)
                Thread.Sleep(100);

            _queue.Verify(p => p.Dequeue(It.IsAny<CancellationToken>()), Times.AtLeast(3));

            foreach (var file in files)
            {
                Assert.False(_fileSystem.File.Exists(file.FilePath));
            }
            Assert.True(_fileSystem.Directory.Exists("/payloads"));

            _logger.VerifyLogging("Cancellation requested.", LogLevel.Information, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Shall delete directories if empty")]
        public async Task ShallDeleteDirectoriesIfEmpty()
        {
            var files = new List<FileStorageInfo>() {
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.1/file1" },
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.2/file2" },
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.2/dir1.2.1/file4" },
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.2/dir1.2.1/file5" },
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.2/dir1.2.2/file6" },
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.2/file3" },
                new FileStorageInfo{  FilePath ="/payloads/dir1/dir1.3/file7" },
                new FileStorageInfo{  FilePath ="/payloads/dir2/dir2.1/dir2.1.1/file1.exe" }
            };
            foreach (var file in files)
            {
                _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(file.FilePath));
                _fileSystem.File.Create(file.FilePath);
            }

            var stack = new Stack<FileStorageInfo>(files.Where(p => !p.FilePath.EndsWith("file3")));
            _queue.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (stack.TryPop(out FileStorageInfo result))
                        return result;

                    _cancellationTokenSource.Cancel();
                    return null;
                });

            await _service.StartAsync(_cancellationTokenSource.Token);
            while (!_cancellationTokenSource.IsCancellationRequested)
                Thread.Sleep(100);

            _queue.Verify(p => p.Dequeue(It.IsAny<CancellationToken>()), Times.AtLeast(7));

            Assert.False(_fileSystem.File.Exists("/payloads/dir1/dir1.1/file1"));
            Assert.False(_fileSystem.File.Exists("/payloads/dir1/dir1.2/file2"));
            Assert.False(_fileSystem.File.Exists("/payloads/dir1/dir1.2/dir1.2.1/file4"));
            Assert.False(_fileSystem.File.Exists("/payloads/dir1/dir1.2/dir1.2.1/file5"));
            Assert.False(_fileSystem.File.Exists("/payloads/dir1/dir1.2/dir1.2.2/file6"));
            Assert.False(_fileSystem.File.Exists("/payloads/dir2/dir2.1/dir2.1.1/file1.exe"));
            Assert.False(_fileSystem.File.Exists("/payloads/dir1/dir1.3/file7"));
            Assert.False(_fileSystem.Directory.Exists("/payloads/dir1/dir1.3"));
            Assert.False(_fileSystem.Directory.Exists("/payloads/dir2"));

            Assert.True(_fileSystem.File.Exists("/payloads/dir1/dir1.2/file3"));
            Assert.True(_fileSystem.Directory.Exists("/payloads/dir1/dir1.2"));
            Assert.True(_fileSystem.Directory.Exists("/payloads/dir1"));
            Assert.True(_fileSystem.Directory.Exists("/payloads"));

            _logger.VerifyLogging("Cancellation requested.", LogLevel.Information, Times.Once());
        }
    }
}
