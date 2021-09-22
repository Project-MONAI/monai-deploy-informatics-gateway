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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Export
{
    public class TestExportService : ExportServiceBase
    {
        public static string AgentName = "TestAgent";

        public event EventHandler ExportDataBlockCalled;

        public bool ConvertReturnsEmpty = false;
        public bool ExportShallFail = false;
        protected override string Agent => AgentName;
        protected override int Concurrentcy => 1;
        public override string ServiceName { get => "Test Export Service"; }

        public TestExportService(
            ILogger logger,
            IOptions<InformaticsGatewayConfiguration> InformaticsGatewayConfiguration,
            IServiceScopeFactory serviceScopeFactory,
            IStorageInfoProvider storageInfoProvider)
            : base(logger, InformaticsGatewayConfiguration, serviceScopeFactory, storageInfoProvider)
        {
        }

        protected override Task<OutputJob> ExportDataBlockCallback(OutputJob outputJob, CancellationToken cancellationToken)
        {
            if (ExportDataBlockCalled != null)
            {
                ExportDataBlockCalled(this, new EventArgs());
            }

            if (ExportShallFail || outputJob is null)
            {
                outputJob.State = State.Failed;
            }
            else
            {
                outputJob.State = State.Succeeded;
            }

            return Task.FromResult(outputJob);
        }
    }

    public class ExportServiceBaseTest
    {
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IWorkloadManagerApi> _workloadmanagerApi;
        private readonly Random _random;

        public ExportServiceBaseTest()
        {
            _logger = new Mock<ILogger>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _configuration.Value.Export.PollFrequencyMs = 10;
            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _workloadmanagerApi = new Mock<IWorkloadManagerApi>();
            _random = new Random();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IWorkloadManagerApi)))
                .Returns(_workloadmanagerApi.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
        }

        [RetryFact(5, 250, DisplayName = "Data flow test - no pending tasks")]
        public async Task DataflowTest_NoPendingTasks()
        {
            var exportCalled = false;
            var convertCalled = false;
            var completedEvent = new ManualResetEvent(false);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            service.ExportDataBlockCalled += (sender, args) =>
            {
                exportCalled = true;
            };
            _workloadmanagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult((IList<TaskResponse>)null));
            _workloadmanagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadmanagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            await service.StartAsync(_cancellationTokenSource.Token);
            Thread.Sleep(3000);
            Assert.False(exportCalled);
            Assert.False(convertCalled);

            _workloadmanagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            await StopAndVerify(service);
            _logger.VerifyLogging($"Export Service completed timer routine.", LogLevel.Trace, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableForExport, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
        }

        [RetryFact(10, 10, DisplayName = "Data flow test - insufficient storage space")]
        public async Task DataflowTest_InsufficientStorageSpace()
        {
            var exportCalled = false;
            var convertCalled = false;
            var completedEvent = new ManualResetEvent(false);
            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            service.ExportDataBlockCalled += (sender, args) =>
            {
                exportCalled = true;
            };
            _workloadmanagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult((IList<TaskResponse>)null));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(false);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            await service.StartAsync(_cancellationTokenSource.Token);
            Thread.Sleep(1000);
            Assert.False(exportCalled);
            Assert.False(convertCalled);

            _workloadmanagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.Never());
            await StopAndVerify(service);
            _logger.VerifyLogging($"Export Service completed timer routine.", LogLevel.Trace, Times.Never());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableForExport, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.AtLeastOnce());
        }

        [RetryFact(5, 250, DisplayName = "Data flow test - payload download failure")]
        public async Task DataflowTest_PayloadDownloadFailure()
        {
            var exportCountdown = new CountdownEvent(1);
            var reportCountdown = new CountdownEvent(1);

            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            service.ExportDataBlockCalled += (sender, args) =>
            {
                exportCountdown.Signal();
            };
            service.ReportActionStarted += (sender, args) =>
            {
                reportCountdown.Signal();
            };

            var tasks = GenerateTaskResponse(1);

            _workloadmanagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadmanagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadmanagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadmanagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(exportCountdown.Wait(3000));
            Assert.False(reportCountdown.Wait(3000));

            _workloadmanagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            _workloadmanagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Failed to download file.", LogLevel.Error, Times.AtLeastOnce());
            await StopAndVerify(service);
            _logger.VerifyLogging($"Error occurred while exporting.", LogLevel.Error, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableForExport, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
        }

        [RetryTheory(10, 10, DisplayName = "Data flow test - completed entire data flow")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DataflowTest_CompletedEntireDataflow(bool exportShallFail)
        {
            var exportCountdown = new CountdownEvent(1);

            var service = new TestExportService(_logger.Object, _configuration, _serviceScopeFactory.Object, _storageInfoProvider.Object);
            service.ExportShallFail = exportShallFail;

            service.ExportDataBlockCalled += (sender, args) =>
            {
                exportCountdown.Signal();
            };

            var tasks = GenerateTaskResponse(1);

            var bytes = new byte[10];
            _random.NextBytes(bytes);
            _workloadmanagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadmanagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadmanagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadmanagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(bytes));
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            await service.StartAsync(_cancellationTokenSource.Token);
            Assert.True(exportCountdown.Wait(5000));
            await Task.Delay(500);
            _workloadmanagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            if (exportShallFail)
            {
                _workloadmanagerApi.Verify(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once());
            }
            else
            {
                _workloadmanagerApi.Verify(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once());
            }
            _workloadmanagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Failed to download file.", LogLevel.Warning, Times.Never());
            _logger.VerifyLogging($"Failure rate exceeded threshold and will not be exported.", LogLevel.Error, Times.Never());
            _logger.VerifyLogging($"Task marked as successful.", LogLevel.Error, Times.Never());

            await StopAndVerify(service);
            _logger.VerifyLogging($"Export Service completed timer routine.", LogLevel.Trace, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableForExport, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
        }

        internal static IList<TaskResponse> GenerateTaskResponse(int count)
        {
            var result = new List<TaskResponse>();

            for (int i = 0; i < count; i++)
            {
                result.Add(new TaskResponse
                {
                    ApplicationId = Guid.NewGuid().ToString(),
                    Sink = TestExportService.AgentName,
                    ExportTaskId = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid().ToString(),
                    FileId = Guid.NewGuid(),
                    Parameters = JsonConvert.SerializeObject("ABC"),
                    Retries = 0,
                    State = State.InProgress
                });
            }

            return result;
        }

        private async Task StopAndVerify(TestExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _workloadmanagerApi.Invocations.Clear();
            _logger.VerifyLogging($"Export Task Watcher Hosted Service is stopping.", LogLevel.Information, Times.Once());
            Thread.Sleep(500);
            _workloadmanagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}
