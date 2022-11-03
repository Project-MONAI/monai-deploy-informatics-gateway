/*
 * Copyright 2021-2022 MONAI Consortium
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadAssemblerTest
    {
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly Mock<ILogger<PayloadAssembler>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        private readonly Mock<IInformaticsGatewayRepository<Payload>> _repository;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public PayloadAssemblerTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _repository = new Mock<IInformaticsGatewayRepository<Payload>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());
            _logger = new Mock<ILogger<PayloadAssembler>>();
            _cancellationTokenSource = new CancellationTokenSource();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInformaticsGatewayRepository<Payload>)))
                .Returns(_repository.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1, 1, 1 };
        }

        [Fact]
        public void GivenAPayloadAssembler_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new PayloadAssembler(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadAssembler(_options, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadAssembler(_options, _logger.Object, null));
        }

        [RetryFact(10, 200)]
        public async Task GivenAFileStorageMetadata_WhenQueueingWihtoutSpecifyingATimeout_ExpectDefaultTimeoutToBeUsed()
        {
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            await payloadAssembler.Queue("A", new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt"));

            _logger.VerifyLogging($"Bucket A created with timeout {PayloadAssembler.DEFAULT_TIMEOUT}s.", LogLevel.Information, Times.Once());
            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();
        }

        [RetryFact(10, 200)]
        public async Task GivenFileStorageMetadataInTheDatabase_AtServiceStartup_ExpectPayloadsInCreatedStateToBeRemoved()
        {
            var dataset = new List<Payload>
            {
                new Payload("created-test1", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Created },
                new Payload("created-test2", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Created },
                new Payload("upload-test", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Move },
                new Payload("notify-test", Guid.NewGuid().ToString(), 10) { State = Payload.PayloadState.Notify },
            };

            _repository.Setup(p => p.AsQueryable()).Returns(dataset.AsQueryable());
            _repository.Setup(p => p.Remove(It.IsAny<Payload>()));

            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);
            await Task.Delay(250);
            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();

            _repository.Verify(p => p.Remove(It.IsAny<Payload>()), Times.Exactly(2));
        }

        [RetryFact(10, 200)]
        public async Task GivenAPayloadAssembler_WhenDisposed_ExpectResourceToBeCleanedUp()
        {
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            _ = Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.Run(() => payloadAssembler.Dequeue(_cancellationTokenSource.Token)));

            await payloadAssembler.Queue("A", new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt"));

            payloadAssembler.Dispose();
            _cancellationTokenSource.Cancel();

            await Task.Delay(1000);
            _logger.VerifyLoggingMessageBeginsWith($"Number of collections in queue", LogLevel.Trace, Times.Never());
        }

        [RetryFact(10, 200)]
        public async Task GivenFileStorageMetadata_WhenQueueingWithDatabaseError_ExpectToRetryXTimes()
        {
            int callCount = 0;
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() =>
            {
                if (++callCount >= _options.Value.Database.Retries.DelaysMilliseconds.Length + 1)
                {
                    _cancellationTokenSource.CancelAfter(1200);
                    return;
                }
                if (callCount != 0)
                {
                    throw new Exception("error");
                }
            });

            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            await payloadAssembler.Queue("A", new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt"), 1);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();
            payloadAssembler.Dispose();

            _logger.VerifyLoggingMessageBeginsWith($"Number of buckets active: 1.", LogLevel.Trace, Times.AtLeastOnce());
        }

        [RetryFact(10, 200)]
        public async Task GivenAPayloadThatHasNotCompleteUploads_WhenProcessedByTimedEvent_ExpectToBeAddedToQueue()
        {
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() =>
            {
                _cancellationTokenSource.CancelAfter(1500);
            });
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            var file = new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt");

            await payloadAssembler.Queue("A", file, 1);
            await Task.Delay(1001);
            payloadAssembler.Dispose();

            _logger.VerifyLoggingMessageBeginsWith($"Number of buckets active: 1.", LogLevel.Trace, Times.AtLeastOnce());
            _logger.VerifyLoggingMessageBeginsWith($"Bucket A sent to processing queue", LogLevel.Information, Times.Never());
        }

        [RetryFact(10, 200)]
        public async Task GivenAPayloadThatHasCompletedUploads_WhenProcessedByTimedEvent_ExpectToBeAddedToQueue()
        {
            var payloadAssembler = new PayloadAssembler(_options, _logger.Object, _serviceScopeFactory.Object);

            var file = new TestStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "file1", ".txt");
            file.File.SetUploaded("bucket");
            await payloadAssembler.Queue("A", file, 1);
            await Task.Delay(1001);
            var result = payloadAssembler.Dequeue(_cancellationTokenSource.Token);
            payloadAssembler.Dispose();

            _logger.VerifyLoggingMessageBeginsWith($"Number of buckets active: 1.", LogLevel.Trace, Times.AtLeastOnce());
            Assert.Single(result.Files);
            _logger.VerifyLoggingMessageBeginsWith($"Bucket A sent to processing queue with {result.Count} files", LogLevel.Information, Times.AtLeastOnce());
        }
    }
}
