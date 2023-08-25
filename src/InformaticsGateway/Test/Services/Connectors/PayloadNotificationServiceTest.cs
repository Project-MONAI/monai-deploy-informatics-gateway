/*
 * Copyright 2021-2023 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Events;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadNotificationServiceTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<PayloadNotificationService>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IMessageBrokerPublisherService> _messageBrokerPublisherService;
        private readonly Mock<IPayloadNotificationActionHandler> _payloadNotificationActionHandler;
        private readonly Mock<IPayloadMoveActionHandler> _payloadMoveActionHandler;
        private readonly Mock<IPayloadRepository> _payloadRepository;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public PayloadNotificationServiceTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<PayloadNotificationService>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _payloadAssembler = new Mock<IPayloadAssembler>();
            _messageBrokerPublisherService = new Mock<IMessageBrokerPublisherService>();
            _payloadNotificationActionHandler = new Mock<IPayloadNotificationActionHandler>();
            _payloadMoveActionHandler = new Mock<IPayloadMoveActionHandler>();
            _payloadRepository = new Mock<IPayloadRepository>();

            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScope = new Mock<IServiceScope>();

            var services = new ServiceCollection();
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _messageBrokerPublisherService.Object);
            services.AddScoped(p => _payloadNotificationActionHandler.Object);
            services.AddScoped(p => _payloadMoveActionHandler.Object);
            services.AddScoped(p => _payloadRepository.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1 };
            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 1 };
            _options.Value.Storage.StorageServiceBucketName = "bucket";
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(10, 200)]
        public void GivenAPayloadNotificationService_AtInitialization_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationService(_serviceScopeFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, null));
        }

        [RetryFact(10, 200)]
        public async Task GivenThePayloadNotificationService_WhenStopAsyncIsCalled_ExpectServiceToStopAnyProcessing()
        {
            var payload = new Payload("test", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 100) { State = Payload.PayloadState.Move };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Task.Delay(100).Wait();
                    return payload;
                });

            _payloadRepository.Setup(p => p.GetPayloadsInStateAsync(It.IsAny<CancellationToken>(), It.IsAny<Payload.PayloadState[]>())).ReturnsAsync(new List<Payload>());
            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            await service.StartAsync(_cancellationTokenSource.Token);
            await service.StopAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.CancelAfter(150);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Waiting for {service.ServiceName} to stop.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Uploading payload {payload.PayloadId} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Never());
        }

        [RetryFact(10, 200)]
        public void GivenPayloadsStoredInTheDatabase_WhenServiceStarts_ExpectThePayloadsToBeRestoredAsync()
        {
            var testData = new List<Payload>
            {
                new Payload("created-test", Guid.NewGuid().ToString(),Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 10){ State = Payload.PayloadState.Created},
                new Payload("upload-test", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" },10){ State = Payload.PayloadState.Move},
                new Payload("notification-test", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" },10) {State = Payload.PayloadState.Notify},
            };

            _payloadRepository.Setup(p => p.GetPayloadsInStateAsync(It.IsAny<CancellationToken>(), It.IsAny<Payload.PayloadState[]>()))
                .Callback(() => _cancellationTokenSource.CancelAfter(500))
                .ReturnsAsync(testData);

            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            _ = service.StartAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _payloadMoveActionHandler.Verify(p => p.MoveFilesAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            _payloadNotificationActionHandler.Verify(p => p.NotifyAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        }

        [RetryFact(10, 200)]
        public void GivenAPayload_WhenDequedFromPayloadAssemblerAndFailedToBeProcessByTheMoveActionHandler()
        {
            var resetEvent = new ManualResetEventSlim();
            var payload = new Payload("test", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 100) { State = Payload.PayloadState.Move };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(payload);

            _payloadMoveActionHandler.Setup(p => p.MoveFilesAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()))
                .Callback(() => resetEvent.Set())
                .Throws(new PayloadNotifyException(PayloadNotifyException.FailureReason.IncorrectState));

            _payloadRepository.Setup(p => p.GetPayloadsInStateAsync(It.IsAny<CancellationToken>(), It.IsAny<Payload.PayloadState[]>())).ReturnsAsync(new List<Payload>());
            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            _ = service.StartAsync(_cancellationTokenSource.Token);
            resetEvent.Wait();
        }

        [RetryFact(10, 200)]
        public void GivenAPayload_WhenDequedFromPayloadAssembler_ExpectThePayloadBeProcessedByTheMoveActionHandler()
        {
            var payload = new Payload("test", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new DataOrigin { DataService = Messaging.Events.DataService.DIMSE, Destination = "dest", Source = "source" }, 100) { State = Payload.PayloadState.Move };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>())).Returns(payload);
            _payloadRepository.Setup(p => p.GetPayloadsInStateAsync(It.IsAny<CancellationToken>(), It.IsAny<Payload.PayloadState[]>())).ReturnsAsync(new List<Payload>());

            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            _cancellationTokenSource.CancelAfter(100);
            _ = service.StartAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _payloadMoveActionHandler.Verify(p => p.MoveFilesAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Payload {payload.PayloadId} added to {service.ServiceName} for processing.", LogLevel.Information, Times.AtLeastOnce());
        }
    }
}