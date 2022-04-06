// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Storage;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Export
{
    public class ScuExportServiceTest : IClassFixture<DicomScpFixture>
    {
        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<IMessageBrokerSubscriberService> _messageSubscriberService;
        private readonly Mock<IMessageBrokerPublisherService> _messagePublisherService;
        private readonly Mock<ILogger<ScuExportService>> _logger;
        private readonly Mock<ILogger> _scpLogger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IInformaticsGatewayRepository<DestinationApplicationEntity>> _repository;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly DicomScpFixture _dicomScp;
        private readonly int _port = 11104;

        public ScuExportServiceTest(DicomScpFixture dicomScp)
        {
            _dicomScp = dicomScp ?? throw new ArgumentNullException(nameof(dicomScp));

            _storageService = new Mock<IStorageService>();
            _messageSubscriberService = new Mock<IMessageBrokerSubscriberService>();
            _messagePublisherService = new Mock<IMessageBrokerPublisherService>();
            _logger = new Mock<ILogger<ScuExportService>>();
            _scpLogger = new Mock<ILogger>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _dicomToolkit = new Mock<IDicomToolkit>();
            _cancellationTokenSource = new CancellationTokenSource();
            _repository = new Mock<IInformaticsGatewayRepository<DestinationApplicationEntity>>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInformaticsGatewayRepository<DestinationApplicationEntity>)))
                .Returns(_repository.Object);
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
            DicomScpFixture.Logger = _scpLogger.Object;
            _dicomScp.Start(_port);
            _configuration.Value.Export.Retries.DelaysMilliseconds = new[] { 1 };
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "Constructor - throws on null params")]
        public void Constructor_ThrowsOnNullParams()
        {
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, _serviceScopeFactory.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, null));
        }

        [RetryFact(10, 250, DisplayName = "When no destination is defined")]
        public async Task ShallFailWhenNoDestinationIsDefined()
        {
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
                    messageReceivedCallback(CreateMessageReceivedEventArgs(string.Empty));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());
            _logger.VerifyLogging("SCU Export configuration error: Export task does not have destination set.", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "When destination is not configured")]
        public async Task ShallFailWhenDestinationIsNotConfigured()
        {
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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(default(DestinationApplicationEntity));

            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging($"SCU Export configuration error: Specified destination 'pacs' does not exist.", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "Assocation rejected")]
        public async Task AssociationRejected()
        {
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = "ABC", Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
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
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith($"Association aborted with reason", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "C-STORE Failure")]
        public async Task CStoreFailure()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
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
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging("Error while adding DICOM C-STORE request: error", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "Unreachable Server")]
        public async Task UnreachableServer()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "UNKNOWNHOST123456789", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());
            _logger.VerifyLoggingMessageBeginsWith("Association aborted with error", LogLevel.Error, Times.Once());
        }

        [RetryFact(10, 250, DisplayName = "C-STORe success")]
        public async Task ExportCompletes()
        {
            _scpLogger.Invocations.Clear();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.s_aETITLE, Name = DicomScpFixture.s_aETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

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
                    messageReceivedCallback(CreateMessageReceivedEventArgs("pacs"));
                });

            _storageService.Setup(p => p.GetObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<Stream>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, Action<Stream>, CancellationToken>((bucketName, objectName, callback, cancellationToken) =>
                {
                    callback(new MemoryStream(Encoding.UTF8.GetBytes("test")));
                });

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteMessage>()).Status == ExportStatus.Success)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.Reject(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());
            _logger.VerifyLogging("Association accepted.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Instance sent successfully.", LogLevel.Information, Times.Once());
        }

        private static MessageReceivedEventArgs CreateMessageReceivedEventArgs(string destination)
        {
            var exportRequestMessage = new ExportRequestMessage
            {
                ExportTaskId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                Destination = destination,
                Files = new[] { "file1" },
                MessageId = Guid.NewGuid().ToString(),
                WorkflowId = Guid.NewGuid().ToString(),
            };
            var jsonMessage = new JsonMessage<ExportRequestMessage>(exportRequestMessage, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequestMessage.CorrelationId, exportRequestMessage.DeliveryTag);

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
