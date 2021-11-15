using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class FileStoredNotificationQueueTest
    {
        private readonly Mock<ILogger<FileStoredNotificationQueue>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IInformaticsGatewayRepository<FileStorageInfo>> _repository;
        private readonly MockFileSystem _fileSystem;

        public FileStoredNotificationQueueTest()
        {
            _logger = new Mock<ILogger<FileStoredNotificationQueue>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _fileSystem = new MockFileSystem();
            _repository = new Mock<IInformaticsGatewayRepository<FileStorageInfo>>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInformaticsGatewayRepository<FileStorageInfo>)))
                .Returns(_repository.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
        }

        [RetryFact(5, 250, DisplayName = "Shall load existing stored files from database")]
        public void ShallLoadExistingStoredFileRecords()
        {
            var existingFiles = new List<FileStorageInfo>
            {
                new FileStorageInfo(Guid.NewGuid().ToString(), "/storage", "message1", ".ext", _fileSystem),
                new FileStorageInfo(Guid.NewGuid().ToString(), "/storage", "message2", ".ext", _fileSystem),
            };

            _repository.Setup(p => p.AsQueryable()).Returns(existingFiles.AsQueryable());

            var queue = new FileStoredNotificationQueue(_logger.Object, _serviceScopeFactory.Object);

            _logger.VerifyLoggingMessageBeginsWith($"Adding existing file to queue", LogLevel.Debug, Times.Exactly(existingFiles.Count));
        }

        [RetryFact(5, 250, DisplayName = "Queue and Dequeue")]
        public async Task QueueAndDequeue()
        {
            var queue = new FileStoredNotificationQueue(_logger.Object, _serviceScopeFactory.Object);

            var expected = new FileStorageInfo(Guid.NewGuid().ToString(), "/storage", "message1", ".ext", _fileSystem);
            await queue.Queue(expected);

            var cancellationTokenSource = new CancellationTokenSource();
            var queuedItem = await queue.Dequeue(cancellationTokenSource.Token);
            Assert.Equal(expected, queuedItem);
        }

        [RetryFact(5, 250, DisplayName = "Dequeue - is cancellable")]
        public async Task Dequeue_CanBeCancelled()
        {
            var queue = new FileStoredNotificationQueue(_logger.Object, _serviceScopeFactory.Object);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(50);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await queue.Dequeue(cancellationTokenSource.Token));
        }
    }
}
