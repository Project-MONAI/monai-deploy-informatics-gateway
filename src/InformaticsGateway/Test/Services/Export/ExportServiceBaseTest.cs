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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Storage.API;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Export
{
    public class TestExportService : ExportServiceBase
    {
        public static readonly string AgentName = "TestAgent";

        public event EventHandler ExportDataBlockCalled;

        public bool ExportShallFail = false;
        protected override ushort Concurrency => 1;
        public override string RoutingKey => AgentName;
        public override string ServiceName { get => "Test Export Service"; }

        public TestExportService(
            ILogger logger,
            IOptions<InformaticsGatewayConfiguration> InformaticsGatewayConfiguration,
            IServiceScopeFactory serviceScopeFactory)
            : base(logger, InformaticsGatewayConfiguration, serviceScopeFactory)
        {
        }

        protected override Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            ExportDataBlockCalled?.Invoke(exportRequestData, new EventArgs());

            if (ExportShallFail || exportRequestData is null)
            {
                exportRequestData.SetFailed(FileExportStatus.ServiceError, "Failed");
            }

            return Task.FromResult(exportRequestData);
        }
    }

    public class ExportServiceBaseTest
    {
        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<IMessageBrokerSubscriberService> _messageSubscriberService;
        private readonly Mock<IMessageBrokerPublisherService> _messagePublisherService;
        private readonly Mock<IOutputDataPlugInEngine> _outputDataPlugInEngine;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        public ExportServiceBaseTest()
        {
            _storageService = new Mock<IStorageService>();
            _messageSubscriberService = new Mock<IMessageBrokerSubscriberService>();
            _messagePublisherService = new Mock<IMessageBrokerPublisherService>();
            _outputDataPlugInEngine = new Mock<IOutputDataPlugInEngine>();
            _logger = new Mock<ILogger>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();

            var services = new ServiceCollection();
            services.AddScoped(p => _messagePublisherService.Object);
            services.AddScoped(p => _messageSubscriberService.Object);
            services.AddScoped(p => _outputDataPlugInEngine.Object);
            services.AddScoped(p => _storageService.Object);
            services.AddScoped(p => _storageInfoProvider.Object);

            var serviceProvider = services.BuildServiceProvider();

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);

            _outputDataPlugInEngine.Setup(p => p.Configure(It.IsAny<IReadOnlyList<string>>()));
            _outputDataPlugInEngine.Setup(p => p.ExecutePlugInsAsync(It.IsAny<ExportRequestDataMessage>()))
                .Returns<ExportRequestDataMessage>((ExportRequestDataMessage message) => Task.FromResult(message));

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "Data flow test - can start/stop")]
        public async Task DataflowTest_StartStop()
        {
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object);
            await service.StartAsync(_cancellationTokenSource.Token);
            await StopAndVerify(service);

            _logger.VerifyLogging($"{service.ServiceName} subscribed to {service.RoutingKey} messages.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
        }

        [RetryFact(10, 10, DisplayName = "Data flow test - reject on insufficient storage space")]
        public async Task DataflowTest_RejectOnInsufficientStorageSpace()
        {
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(false);

            _messageSubscriberService.Setup(p => p.Reject(It.IsAny<MessageBase>(), It.IsAny<bool>()));
            _messageSubscriberService.Setup(
                p => p.SubscribeAsync(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                 It.IsAny<ushort>()))
                .Callback((string topic, string queue, Func<MessageReceivedEventArgs, Task> messageReceivedCallback, ushort prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs());
                });

            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object);
            await service.StartAsync(_cancellationTokenSource.Token);
            await StopAndVerify(service);

            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<Message>(), It.IsAny<bool>()), Times.Once());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());
        }

        [RetryFact(10, 10, DisplayName = "Data flow test - payload download failure")]
        public async Task DataflowTest_PayloadDownlaodFailure()
        {
            _configuration.Value.Export.Retries.DelaysMilliseconds = new[] { 1 };

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.SubscribeAsync(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Func<MessageReceivedEventArgs, Task>, ushort>(async (topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    await messageReceivedCallback(CreateMessageReceivedEventArgs());
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("storage error"));

            var countdownEvent = new CountdownEvent(1);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object);
            service.ReportActionCompleted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(countdownEvent.Wait(3000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());
        }

        [RetryFact(1, 10, DisplayName = "Data flow test - end to end workflow with partial failure")]
        public async Task DataflowTest_EndToEnd_WithPartialFailure()
        {
            var messageCount = 5;
            var testData = "this is a test";

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.SubscribeAsync(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Func<MessageReceivedEventArgs, Task>, ushort>(async (topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    while (messageCount-- > 0)
                    {
                        await messageReceivedCallback(CreateMessageReceivedEventArgs());
                    }
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(testData)));

            var countdownEvent = new CountdownEvent(5 * 3);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object);
            service.ReportActionCompleted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            service.ExportDataBlockCalled += (sender, e) =>
            {
                var data = sender as ExportRequestDataMessage;
                Assert.Equal(testData, Encoding.UTF8.GetString(data.FileContent));

                service.ExportShallFail = data.Filename == "file1";
                countdownEvent.Signal();
            };
            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(countdownEvent.Wait(1000000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == ExportStatus.PartialFailure)), Times.Exactly(5));
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Exactly(5));
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());
        }

        [RetryFact(1, 10, DisplayName = "Data flow test - end to end workflow")]
        public async Task DataflowTest_EndToEnd()
        {
            var messageCount = 5;
            var testData = "this is a test";

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.SubscribeAsync(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Func<MessageReceivedEventArgs, Task>, ushort>(async (topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    while (messageCount-- > 0)
                    {
                        await messageReceivedCallback(CreateMessageReceivedEventArgs());
                    }
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(testData)));

            var countdownEvent = new CountdownEvent(5 * 3);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object);
            service.ReportActionCompleted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            service.ExportDataBlockCalled += (sender, e) =>
            {
                var data = sender as ExportRequestDataMessage;
                Assert.Equal(testData, Encoding.UTF8.GetString(data.FileContent));
                countdownEvent.Signal();
            };
            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(countdownEvent.Wait(1000000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == ExportStatus.Success)), Times.Exactly(5));
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Exactly(5));
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());
            _outputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Exactly(5 * 2));
            _outputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<ExportRequestDataMessage>()), Times.Exactly(5 * 2));
        }

        internal static MessageReceivedEventArgs CreateMessageReceivedEventArgs()
        {
            var exportRequestEvent = new ExportRequestEvent
            {
                ExportTaskId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                Destinations = new[] { "destination" },
                Files = new[] { "file1", "file2" },
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };

            var jsonMessage = new JsonMessage<ExportRequestEvent>(exportRequestEvent, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequestEvent.CorrelationId, exportRequestEvent.DeliveryTag);

            return new MessageReceivedEventArgs(jsonMessage.ToMessage(), CancellationToken.None);
        }

        private async Task StopAndVerify(TestExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
            Thread.Sleep(250);
        }
    }
}
