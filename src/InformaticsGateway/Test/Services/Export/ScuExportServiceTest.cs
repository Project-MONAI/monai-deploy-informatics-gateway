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
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
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
    [Collection("SCP Listener")]
    public class ScuExportServiceTest
    {
        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<IMessageBrokerSubscriberService> _messageSubscriberService;
        private readonly Mock<IMessageBrokerPublisherService> _messagePublisherService;
        private readonly Mock<IOutputDataPlugInEngine> _outputDataPlugInEngine;
        private readonly Mock<ILogger<ScuExportService>> _logger;
        private readonly Mock<ILogger> _scpLogger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IDestinationApplicationEntityRepository> _repository;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly DicomScpFixture _dicomScp;
        private readonly int _port = 1104;

        public ScuExportServiceTest(DicomScpFixture dicomScp)
        {
            _dicomScp = dicomScp ?? throw new ArgumentNullException(nameof(dicomScp));

            _storageService = new Mock<IStorageService>();
            _messageSubscriberService = new Mock<IMessageBrokerSubscriberService>();
            _messagePublisherService = new Mock<IMessageBrokerPublisherService>();
            _outputDataPlugInEngine = new Mock<IOutputDataPlugInEngine>();
            _logger = new Mock<ILogger<ScuExportService>>();
            _scpLogger = new Mock<ILogger>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _dicomToolkit = new Mock<IDicomToolkit>();
            _cancellationTokenSource = new CancellationTokenSource();
            _repository = new Mock<IDestinationApplicationEntityRepository>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();

            var services = new ServiceCollection();
            services.AddScoped(p => _repository.Object);
            services.AddScoped(p => _messagePublisherService.Object);
            services.AddScoped(p => _messageSubscriberService.Object);
            services.AddScoped(p => _outputDataPlugInEngine.Object);
            services.AddScoped(p => _storageService.Object);
            services.AddScoped(p => _storageInfoProvider.Object);

            var serviceProvider = services.BuildServiceProvider();

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
            DicomScpFixture.Logger = _scpLogger.Object;
            _dicomScp.Start(_port);
            _configuration.Value.Export.Retries.DelaysMilliseconds = new[] { 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _outputDataPlugInEngine.Setup(p => p.Configure(It.IsAny<IReadOnlyList<string>>()));
            _outputDataPlugInEngine.Setup(p => p.ExecutePlugInsAsync(It.IsAny<ExportRequestDataMessage>()))
                .Returns<ExportRequestDataMessage>((ExportRequestDataMessage message) => Task.FromResult(message));
        }

        [RetryFact(5, 250, DisplayName = "Constructor - throws on null params")]
        public void Constructor_ThrowsOnNullParams()
        {
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, _serviceScopeFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, null));
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

            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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
            _logger.VerifyLogging("SCU Export configuration error: Export task does not have destination set.", LogLevel.Error, Times.Once());
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

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(DestinationApplicationEntity));

            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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

            _logger.VerifyLogging($"SCU Export configuration error: Specified destination 'pacs' does not exist.", LogLevel.Error, Times.Once());
        }

        [RetryFact(1, 250, DisplayName = "Association rejected")]
        public async Task AssociationRejected()
        {
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = "ABC", Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };

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

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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

            _logger.VerifyLogging($"Association rejected.", LogLevel.Warning, Times.AtLeastOnce());
            _logger.VerifyLoggingMessageBeginsWith($"Association rejected with reason", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "C-STORE simulate abort")]
        public async Task SimulateAbort()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = "ABORT", Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            DicomScpFixture.DicomStatus = DicomStatus.ResourceLimitation;
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

            _logger.VerifyLoggingMessageBeginsWith($"Association aborted with reason", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "C-STORE Failure")]
        public async Task CStoreFailure()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionCompleted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            DicomScpFixture.DicomStatus = DicomStatus.ResourceLimitation;
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
            _logger.VerifyLogging("Association accepted.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Failed to export with error {DicomStatus.ResourceLimitation}.", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "Failed to load DICOM content")]
        public async Task ErrorLoadingDicomContent()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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

            _repository.Setup(p => p.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Throws(new Exception("error"));

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
                               It.Is<Message>(match => CheckMessage(match, ExportStatus.Failure, FileExportStatus.UnsupportedDataType))), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error reading DICOM file: error", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "Unreachable Server")]
        public async Task UnreachableServer()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "UNKNOWNHOST123456789", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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
            Assert.True(dataflowCompleted.WaitOne(8000));
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
            _logger.VerifyLoggingMessageBeginsWith("Association aborted with error", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "C-STORE success")]
        public async Task ExportCompletes()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _dicomToolkit.Object);

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
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.SubscribeAsync(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Func<MessageReceivedEventArgs, Task>>(),
                                                              It.IsAny<ushort>()), Times.Once());
            _logger.VerifyLogging("Association accepted.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Instance sent successfully.", LogLevel.Information, Times.Once());
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
                Destinations = new[] { destination },
                Files = new[] { "file1" },
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };
            var jsonMessage = new JsonMessage<ExportRequestEvent>(exportRequestEvent, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequestEvent.CorrelationId, exportRequestEvent.DeliveryTag);

            return new MessageReceivedEventArgs(jsonMessage.ToMessage(), CancellationToken.None);
        }

        private async Task StopAndVerify(ScuExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
            Thread.Sleep(500);
        }
    }
}
