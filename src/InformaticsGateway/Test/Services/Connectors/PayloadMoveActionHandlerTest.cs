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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.Storage.API;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadMoveActionHandlerTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<PayloadMoveActionHandler>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<IPayloadRepository> _repository;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PayloadMoveActionHandlerTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<PayloadMoveActionHandler>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _storageService = new Mock<IStorageService>();
            _repository = new Mock<IPayloadRepository>();

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _storageService.Object);
            services.AddScoped(p => _repository.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _cancellationTokenSource = new CancellationTokenSource();

            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 5, 5, 5 };
            _options.Value.Storage.StorageServiceBucketName = "bucket";

            _storageService.Setup(p => p.VerifyObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        [RetryFact(10, 200)]
        public void GivenAPayloadMoveActionHandler_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new PayloadMoveActionHandler(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadMoveActionHandler(_serviceScopeFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadMoveActionHandler(_serviceScopeFactory.Object, _logger.Object, null));

            _ = new PayloadMoveActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);
        }

        [RetryFact(10, 200)]
        public async Task GivenAPayloadInIncorrectState_WhenHandlerIsCalled_ExpectExceptionToBeThrown()
        {
            var resetEvent = new ManualResetEventSlim();
            var moveAction = new ActionBlock<Payload>(payload =>
            {
            });
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
            });

            var correlationId = Guid.NewGuid();
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                State = Payload.PayloadState.Created,
                Files = new List<FileStorageMetadata>
                 {
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                 },
            };

            var handler = new PayloadMoveActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await Assert.ThrowsAsync<PayloadNotifyException>(async () => await handler.MoveFilesAsync(payload, moveAction, notifyAction, _cancellationTokenSource.Token));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task GivenAPayload_WhenHandlerFailedToCopyFiles_ExpectToBePutBackInMoveQueue(int retryCount)
        {
            var resetEvent = new ManualResetEventSlim();
            var moveAction = new ActionBlock<Payload>(payload =>
            {
                resetEvent.Set();
            });
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
            });
            _storageService.Setup(p => p.CopyObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            var correlationId = Guid.NewGuid();
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                RetryCount = retryCount,
                State = Payload.PayloadState.Move,
                Files = new List<FileStorageMetadata>
                 {
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                 },
            };

            var handler = new PayloadMoveActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await handler.MoveFilesAsync(payload, moveAction, notifyAction, _cancellationTokenSource.Token);

            Assert.True(resetEvent.Wait(TimeSpan.FromSeconds(3)));
            Assert.Equal(retryCount + 1, payload.RetryCount);
        }

        [RetryFact(10, 200)]
        public async Task GivenAPayloadThatHasReachedMaximumRetries_WhenHandlerFailedToCopyFiles_ExpectPayloadToBeDeleted()
        {
            var moveAction = new ActionBlock<Payload>(payload =>
            {
            });
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
            });
            _storageService.Setup(p => p.CopyObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            var correlationId = Guid.NewGuid();
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                RetryCount = 3,
                State = Payload.PayloadState.Move,
                Files = new List<FileStorageMetadata>
                 {
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                 },
            };

            var handler = new PayloadMoveActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await handler.MoveFilesAsync(payload, moveAction, notifyAction, _cancellationTokenSource.Token);

            _repository.Verify(p => p.RemoveAsync(payload, _cancellationTokenSource.Token), Times.Once());
        }

        [RetryFact(10, 200)]
        public async Task GivenAPayload_WhenAllFilesAreMove_ExpectPayloadToBeAddedToNotificationQueue()
        {
            var notifyEvent = new ManualResetEventSlim();
            var moveAction = new ActionBlock<Payload>(payload =>
            {
            });
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
                notifyEvent.Set();
            });

            var correlationId = Guid.NewGuid();
            var file1 = new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            file1.File.SetMoved("test");
            file1.JsonFile.SetMoved("test");
            var file2 = new FhirFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Api.Rest.FhirStorageFormat.Json);
            file2.File.SetMoved("test");
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                RetryCount = 3,
                State = Payload.PayloadState.Move,
                Files = new List<FileStorageMetadata>
                 {
                     file1,
                     file2,
                 },
            };


            var handler = new PayloadMoveActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await handler.MoveFilesAsync(payload, moveAction, notifyAction, _cancellationTokenSource.Token);

            Assert.True(notifyEvent.Wait(TimeSpan.FromSeconds(5)));

            //_storageService.Verify(p => p.CopyObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
            //_storageService.Verify(p => p.RemoveObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }
    }
}
