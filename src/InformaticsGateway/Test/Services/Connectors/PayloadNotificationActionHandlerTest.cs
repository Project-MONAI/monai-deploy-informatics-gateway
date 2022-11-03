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
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Messages;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadNotificationActionHandlerTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<PayloadNotificationActionHandler>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IMessageBrokerPublisherService> _messageBrokerPublisherService;
        private readonly Mock<IInformaticsGatewayRepository<Payload>> _informaticsGatewayReepository;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PayloadNotificationActionHandlerTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<PayloadNotificationActionHandler>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _messageBrokerPublisherService = new Mock<IMessageBrokerPublisherService>();
            _informaticsGatewayReepository = new Mock<IInformaticsGatewayRepository<Payload>>();

            _serviceScope = new Mock<IServiceScope>();
            var services = new ServiceCollection();
            services.AddScoped(p => _messageBrokerPublisherService.Object);
            services.AddScoped(p => _informaticsGatewayReepository.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _cancellationTokenSource = new CancellationTokenSource();

            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 5, 5, 5 };
            _options.Value.Storage.StorageServiceBucketName = "bucket";
        }

        [Fact]
        public void GivenAPayloadNotificationActionHandler_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationActionHandler(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationActionHandler(_serviceScopeFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationActionHandler(_serviceScopeFactory.Object, _logger.Object, null));

            _ = new PayloadNotificationActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);
        }

        [Fact]
        public async Task GivenAPayloadInIncorrectState_WhenHandlerIsCalled_ExpectExceptionToBeThrown()
        {
            var resetEvent = new ManualResetEventSlim();
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

            var handler = new PayloadNotificationActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await Assert.ThrowsAsync<PayloadNotifyException>(async () => await handler.NotifyAsync(payload, notifyAction, _cancellationTokenSource.Token));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task GivenAPayload_WhenHandlerFailedToPublishNotification_ExpectToBePutBackInMoveQueue(int retryCount)
        {
            var resetEvent = new ManualResetEventSlim();
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
                resetEvent.Set();
            });

            _messageBrokerPublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()))
                .Throws(new Exception("error"));

            var correlationId = Guid.NewGuid();
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                RetryCount = retryCount,
                State = Payload.PayloadState.Notify,
                Files = new List<FileStorageMetadata>
                 {
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                 },
            };

            var handler = new PayloadNotificationActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await handler.NotifyAsync(payload, notifyAction, _cancellationTokenSource.Token);

            Assert.True(resetEvent.Wait(TimeSpan.FromSeconds(3)));
            Assert.Equal(retryCount + 1, payload.RetryCount);
        }

        [Fact]
        public async Task GivenAPayloadThatHasReachedMaximumRetries_WhenHandlerFailedToPublishNotification_ExpectPayloadToBeDeleted()
        {
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
            });

            _messageBrokerPublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()))
                .Throws(new Exception("error"));

            var correlationId = Guid.NewGuid();
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                RetryCount = 3,
                State = Payload.PayloadState.Notify,
                Files = new List<FileStorageMetadata>
                 {
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                 },
            };

            var handler = new PayloadNotificationActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await handler.NotifyAsync(payload, notifyAction, _cancellationTokenSource.Token);

            _informaticsGatewayReepository.Verify(p => p.Remove(payload), Times.Once());
        }

        [Fact]
        public async Task GivenAPayload_WhenMessageIsPublished_ExpectPayloadToBeDeleted()
        {
            var notifyAction = new ActionBlock<Payload>(payload =>
            {
            });

            var correlationId = Guid.NewGuid();
            var payload = new Payload("key", correlationId.ToString(), 0)
            {
                RetryCount = 3,
                State = Payload.PayloadState.Notify,
                Files = new List<FileStorageMetadata>
                 {
                     new DicomFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                     new FhirFileStorageMetadata(correlationId.ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Api.Rest.FhirStorageFormat.Json),
                 },
            };

            var handler = new PayloadNotificationActionHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            await handler.NotifyAsync(payload, notifyAction, _cancellationTokenSource.Token);

            _messageBrokerPublisherService.Verify(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()), Times.AtLeastOnce());
            _informaticsGatewayReepository.Verify(p => p.Remove(It.IsAny<Payload>()), Times.AtLeastOnce());
        }
    }
}
