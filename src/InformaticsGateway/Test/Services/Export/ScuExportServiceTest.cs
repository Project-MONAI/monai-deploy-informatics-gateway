// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Dicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Monai.Deploy.InformaticsGateway.Test.Shared;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Export
{
    public class ScuExportServiceTest : IClassFixture<DicomScpFixture>

    {
        private readonly Mock<ILogger<ScuExportService>> _logger;
        private readonly Mock<ILogger> _scpLogger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IWorkloadManagerApi> _workloadManagerApi;
        private readonly Mock<IInformaticsGatewayRepository<DestinationApplicationEntity>> _repository;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Random _random;
        private readonly DicomScpFixture _dicomScp;
        private readonly int _port = 11104;

        public ScuExportServiceTest(DicomScpFixture dicomScp)
        {
            _dicomScp = dicomScp ?? throw new ArgumentNullException(nameof(dicomScp));

            _logger = new Mock<ILogger<ScuExportService>>();
            _scpLogger = new Mock<ILogger>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _dicomToolkit = new Mock<IDicomToolkit>();
            _workloadManagerApi = new Mock<IWorkloadManagerApi>();
            _cancellationTokenSource = new CancellationTokenSource();
            _random = new Random();
            _repository = new Mock<IInformaticsGatewayRepository<DestinationApplicationEntity>>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IWorkloadManagerApi)))
                .Returns(_workloadManagerApi.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(IInformaticsGatewayRepository<DestinationApplicationEntity>)))
                .Returns(_repository.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
            DicomScpFixture.Logger = _scpLogger.Object;
            _dicomScp.Start(_port);
        }

        [Fact(DisplayName = "When no destination defined in Parameters")]
        public async Task ShallFailWhenNoDestinationIsDefined()
        {
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);
            tasks.First().Parameters = null;

            var bytes = new byte[10];
            _random.NextBytes(bytes);

            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(bytes));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.Dicom.Scu.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Job failed with error: Task Parameter is missing destination.", LogLevel.Error, Times.AtLeastOnce());
            _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());

            await StopAndVerify(service);
        }

        [Fact(DisplayName = "When destination is not configured")]
        public async Task ShallFailWhenDestinationIsNotConfigured()
        {
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);

            var bytes = new byte[10];
            _random.NextBytes(bytes);

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(default(DestinationApplicationEntity));
            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(bytes));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.Dicom.Scu.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Job failed with error: Specified destination 'ABC' does not exist.", LogLevel.Error, Times.AtLeastOnce());
            _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());

            await StopAndVerify(service);
        }

        [Fact(DisplayName = "Assocation rejected")]
        public async Task AssociationRejected()
        {
            var destination = new DestinationApplicationEntity { AeTitle = "ABC", Name = DicomScpFixture.AETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);

            var bytes = new byte[10];
            _random.NextBytes(bytes);
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(bytes));
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));
            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.Dicom.Scu.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());
            _logger.VerifyLogging("Association rejected.", LogLevel.Warning, Times.AtLeastOnce());

            await StopAndVerify(service);
        }

        [Fact(DisplayName = "C-STORE simulate abort")]
        public async Task SimulateAbort()
        {
            _scpLogger.Invocations.Clear();
            var destination = new DestinationApplicationEntity { AeTitle = "ABORT", Name = DicomScpFixture.AETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);
            DicomScpFixture.DicomStatus = Dicom.Network.DicomStatus.ResourceLimitation;
            var bytes = new byte[10];
            _random.NextBytes(bytes);
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(bytes));
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));
            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(7000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.Dicom.Scu.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());
            _logger.VerifyLoggingMessageBeginsWith("Association aborted with reason", LogLevel.Error, Times.AtLeastOnce());
            _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());
            await StopAndVerify(service);
        }

        [Fact(DisplayName = "C-STORE Failure")]
        public async Task CStoreFailure()
        {
            _scpLogger.Invocations.Clear();
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.AETITLE, Name = DicomScpFixture.AETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);
            DicomScpFixture.DicomStatus = Dicom.Network.DicomStatus.ResourceLimitation;
            var bytes = new byte[10];
            _random.NextBytes(bytes);
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(bytes));
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));
            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(7000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.Dicom.Scu.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());
            _logger.VerifyLogging("Association accepted.", LogLevel.Information, Times.AtLeastOnce());
            _logger.VerifyLogging($"Failed to export job {tasks.First().ExportTaskId} with error {Dicom.Network.DicomStatus.ResourceLimitation}", LogLevel.Error, Times.AtLeastOnce());
            _scpLogger.VerifyLogging($"Instance received {sopInstanceUid}", LogLevel.Information, Times.AtLeastOnce());
            await StopAndVerify(service);
        }

        [Fact(DisplayName = "C-STORe success")]
        public async Task ExportCompletes()
        {
            _scpLogger.Invocations.Clear();
            var destination = new DestinationApplicationEntity { AeTitle = DicomScpFixture.AETITLE, Name = DicomScpFixture.AETITLE, HostIp = "localhost", Port = _port };
            var service = new ScuExportService(_logger.Object, _serviceScopeFactory.Object, _configuration, _storageInfoProvider.Object, _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);
            DicomScpFixture.DicomStatus = Dicom.Network.DicomStatus.Success;

            var bytes = new byte[10];
            _random.NextBytes(bytes);
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;

            _repository.Setup(p => p.FirstOrDefault(It.IsAny<Func<DestinationApplicationEntity, bool>>())).Returns(destination);
            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(bytes));
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));
            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(7000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.Dicom.Scu.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Task marked as successful.", LogLevel.Information, Times.AtLeastOnce());
            _logger.VerifyLogging("Association accepted.", LogLevel.Information, Times.AtLeastOnce());
            _logger.VerifyLogging($"Job {tasks.First().ExportTaskId} sent successfully", LogLevel.Information, Times.AtLeastOnce());
            _scpLogger.VerifyLogging($"Instance received {sopInstanceUid}", LogLevel.Information, Times.AtLeastOnce());
            await StopAndVerify(service);
        }

        private async Task StopAndVerify(ScuExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _workloadManagerApi.Invocations.Clear();
            _logger.VerifyLogging($"Export Task Watcher Hosted Service is stopping.", LogLevel.Information, Times.Once());
            Thread.Sleep(500);
            _workloadManagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}