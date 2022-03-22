// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
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
        protected override int Concurrency => 1;
        public override string RoutingKey => AgentName;
        public override string ServiceName { get => "Test Export Service"; }

        public TestExportService(
            ILogger logger,
            IOptions<InformaticsGatewayConfiguration> InformaticsGatewayConfiguration,
            IServiceScopeFactory serviceScopeFactory,
            IStorageInfoProvider storageInfoProvider)
            : base(logger, InformaticsGatewayConfiguration, serviceScopeFactory, storageInfoProvider)
        {
        }

        protected override Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            ExportDataBlockCalled?.Invoke(exportRequestData, new EventArgs());

            if (ExportShallFail || exportRequestData is null)
            {
                exportRequestData.SetFailed("Failed");
            }

            return Task.FromResult(exportRequestData);
        }
    }

    public class ExportServiceBaseTest
    {
        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<IMessageBrokerSubscriberService> _messageSubscriberService;
        private readonly Mock<IMessageBrokerPublisherService> _messagePublisherService;
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
            _logger = new Mock<ILogger>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IMessageBrokerPublisherService)))
                .Returns(_messagePublisherService.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(IMessageBrokerSubscriberService)))
                .Returns(_messageSubscriberService.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(IStorageService)))
                .Returns(_storageService.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "Data flow test - can start/stop")]
        public async Task DataflowTest_StartStop()
        {
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            await service.StartAsync(_cancellationTokenSource.Token);
            await StopAndVerify(service);

            _logger.VerifyLogging($"{service.ServiceName} subscribed to {service.RoutingKey} messages.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
        }

        [RetryFact(10, 10, DisplayName = "Data flow test - reject on insufficient storage space")]
        public async Task DataflowTest_RejectOnInsufficientStorageSpace()
        {
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(false);

            _messageSubscriberService.Setup(p => p.Reject(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback((string topic, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs());
                });

            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            await service.StartAsync(_cancellationTokenSource.Token);
            await StopAndVerify(service);

            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<Message>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());
        }

        [RetryFact(10, 10, DisplayName = "Data flow test - payload download failure")]
        public async Task DataflowTest_PayloadDownlaodFailure()
        {
            _configuration.Value.Export.Retries.DelaysMilliseconds = new[] { 1 };
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.Reject(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Action<MessageReceivedEventArgs>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs());
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(Stream.Null);
                    throw new Exception("storage error");
                });
            var countdownEvent = new CountdownEvent(1);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            service.ReportActionCompleted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(countdownEvent.Wait(3000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());
        }

        [RetryFact(1, 10, DisplayName = "Data flow test - end to end workflow")]
        public async Task DataflowTest_EndToEnd()
        {
            var messageCount = 5;
            var testData = "this is a test";
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.Reject(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Action<MessageReceivedEventArgs>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    while (messageCount-- > 0)
                    {
                        messageReceivedCallback(CreateMessageReceivedEventArgs());
                    }
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes(testData)));
                });

            var countdownEvent = new CountdownEvent(5 * 3);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Success)), Times.Exactly(5));
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Exactly(5));
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());
        }

        internal static MessageReceivedEventArgs CreateMessageReceivedEventArgs()
        {
            var exportRequestMessage = new ExportRequestMessage
            {
                ExportTaskId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                Destination = "destination",
                Files = new[] { "file1", "file2" },
                MessageId = Guid.NewGuid().ToString(),
                WorkflowId = Guid.NewGuid().ToString(),
            };
            var jsonMessage = new JsonMessage<ExportRequestMessage>(exportRequestMessage, exportRequestMessage.CorrelationId, exportRequestMessage.DeliveryTag);

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
