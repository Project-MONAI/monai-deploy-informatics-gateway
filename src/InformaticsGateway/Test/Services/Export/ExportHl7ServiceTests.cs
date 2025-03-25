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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Mllp;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;
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
    [Collection("Hl7 Export Listener")]
    public class ExportHl7ServiceTests
    {
        private readonly Mock<IStorageService> _storageService = new Mock<IStorageService>();
        private readonly Mock<IMessageBrokerSubscriberService> _messageSubscriberService = new Mock<IMessageBrokerSubscriberService>();
        private readonly Mock<IMessageBrokerPublisherService> _messagePublisherService = new Mock<IMessageBrokerPublisherService>();
        private readonly Mock<ILogger<Hl7ExportService>> _logger = new Mock<ILogger<Hl7ExportService>>();
        private readonly Mock<ILogger> _extAppScpLogger = new Mock<ILogger>();
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory = new Mock<IServiceScopeFactory>();
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IDicomToolkit> _dicomToolkit = new Mock<IDicomToolkit>();
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider = new Mock<IStorageInfoProvider>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Mock<IMllpService> _mllpService = new Mock<IMllpService>();
        private readonly Mock<IOutputDataPlugInEngine> _outputDataPlugInEngine = new Mock<IOutputDataPlugInEngine>();
        private readonly Mock<IHL7DestinationEntityRepository> _repository = new Mock<IHL7DestinationEntityRepository>();
        private readonly int _port = 1104;

        public ExportHl7ServiceTests()
        {
            _configuration = Options.Create(new InformaticsGatewayConfiguration());

            var services = new ServiceCollection();
            services.AddScoped(p => _messagePublisherService.Object);
            services.AddScoped(p => _messageSubscriberService.Object);
            services.AddScoped(p => _storageService.Object);
            services.AddScoped(p => _storageInfoProvider.Object);
            services.AddScoped(p => _mllpService.Object);
            services.AddScoped(p => _outputDataPlugInEngine.Object);
            services.AddScoped(p => _repository.Object);

            var serviceProvider = services.BuildServiceProvider();

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
            _configuration.Value.Export.Retries.DelaysMilliseconds = new[] { 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _outputDataPlugInEngine.Setup(p => p.Configure(It.IsAny<IReadOnlyList<string>>()));
            _outputDataPlugInEngine.Setup(p => p.ExecutePlugInsAsync(It.IsAny<ExportRequestDataMessage>()))
                .Returns<ExportRequestDataMessage>((ExportRequestDataMessage message) => Task.FromResult(message));
        }

        [RetryFact(1, 250, DisplayName = "Constructor - throws on null params")]
        public void Constructor_ThrowsOnNullParams()
        {
            Assert.Throws<ArgumentNullException>(() => new Hl7ExportService(null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new Hl7ExportService(_logger.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, null, null));
            Assert.Throws<ArgumentNullException>(() => new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object, null));
        }


        [RetryFact(10, 250, DisplayName = "When no destination is defined")]
        public async Task ShallFailWhenNoDestinationIsDefined()
        {
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
                    await messageReceivedCallback(CreateMessageReceivedEventArgs(string.Empty));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            var service = new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object, _mllpService.Object);

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(dataflowCompleted.WaitOne(3000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => CheckMessage(match, ExportStatus.Failure, FileExportStatus.ConfigurationError))), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());
            _logger.VerifyLogging("Export task does not have destination set.", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "When destination is not configured")]
        public async Task ShallFailWhenDestinationIsNotConfigured()
        {
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
                    await messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(HL7DestinationEntity));

            var service = new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object, _mllpService.Object);

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(dataflowCompleted.WaitOne(3000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => CheckMessage(match, ExportStatus.Failure, FileExportStatus.ConfigurationError))), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging($"Specified destination 'pacs' does not exist.", LogLevel.Error, Times.Once());
        }

        [RetryFact(1, 250, DisplayName = "HL7 message rejected")]
        public async Task No_Ack_Sent()
        {
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new HL7DestinationEntity { HostIp = "192.168.0.0", Port = _port };

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
                    await messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _mllpService.Setup(p => p.SendMllp(It.IsAny<System.Net.IPAddress>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Hl7SendException("Send exception"));

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

            var service = new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object, _mllpService.Object);

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(dataflowCompleted.WaitOne(5000));

            await StopAndVerify(service);
            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => CheckMessage(match, ExportStatus.Failure, FileExportStatus.ServiceError))), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.Verify(x => x.Log(
                LogLevel.Error,
                538, // this is the eventId of the log we're looking for
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));
        }

        [RetryFact(1, 250, DisplayName = "Failed to load message content")]
        public async Task Error_Loading_HL7_Content()
        {

            _extAppScpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new HL7DestinationEntity { HostIp = "192.168.0.0", Port = _port };
            var service = new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object, _mllpService.Object);

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
                    await messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Stream>(null));

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);


            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            DicomScpFixture.DicomStatus = DicomStatus.Success;
            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(dataflowCompleted.WaitOne(5000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                               It.Is<Message>(match => CheckMessage(match, ExportStatus.Failure, FileExportStatus.DownloadError))), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging("Error downloading payload.", LogLevel.Error, Times.AtLeastOnce());
            _logger.VerifyLoggingMessageBeginsWith("Error downloading payload. Waiting ", LogLevel.Error, Times.AtLeastOnce());
        }

        [RetryFact(2, 250, DisplayName = "success after Hl7 send")]
        public async Task Success_After_Hl7Send()
        {
            _extAppScpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new HL7DestinationEntity { HostIp = "192.168.0.0", Port = _port };
            var service = new Hl7ExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object, _mllpService.Object);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.SubscribeAsync(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Func<MessageReceivedEventArgs, Task>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            DicomScpFixture.DicomStatus = DicomStatus.Success;
            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(dataflowCompleted.WaitOne(5000));
            await StopAndVerify(service);

            _messagePublisherService.Verify(
                p => p.Publish(It.IsAny<string>(),
                   It.Is<Message>(match => CheckMessage(match, ExportStatus.Success, FileExportStatus.Success))), Times.Once());
        }




        private bool CheckMessage(Message message, ExportStatus exportStatus, FileExportStatus fileExportStatus)
        {
            Guard.Against.Null(message, nameof(message));

            var exportEvent = message.ConvertTo<ExportCompleteEvent>();
            return exportEvent.Status == exportStatus &&
                    exportEvent.FileStatuses.First().Value == fileExportStatus;
        }

        private static MessageReceivedEventArgs CreateMessageReceivedEventArgs(string destination)
        {
            var exportRequestEvent = new ExportRequestEvent
            {
                ExportTaskId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                Destinations = new string[] { destination },
                Files = new[] { "file1" },
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };
            var jsonMessage = new JsonMessage<ExportRequestEvent>(exportRequestEvent, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequestEvent.CorrelationId, exportRequestEvent.DeliveryTag);

            return new MessageReceivedEventArgs(jsonMessage.ToMessage(), CancellationToken.None);
        }

        private async Task StopAndVerify(Hl7ExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
            await Task.Delay(500);
        }
    }
}
