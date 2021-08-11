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
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using Moq.Protected;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Export
{
    public class DicomWebExportServiceTest
    {
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly Mock<IInferenceRequestRepository> _inferenceRequestStore;
        private readonly Mock<ILogger<DicomWebExportService>> _logger;
        private readonly Mock<IWorkloadManagerApi> _workloadManagerApi;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Mock<HttpMessageHandler> _handlerMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Random _random;

        public DicomWebExportServiceTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _inferenceRequestStore = new Mock<IInferenceRequestRepository>();
            _logger = new Mock<ILogger<DicomWebExportService>>();
            _workloadManagerApi = new Mock<IWorkloadManagerApi>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _configuration.Value.Export.PollFrequencyMs = 10;
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _dicomToolkit = new Mock<IDicomToolkit>();
            _random = new Random();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInferenceRequestRepository)))
                .Returns(_inferenceRequestStore.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(IWorkloadManagerApi)))
                .Returns(_workloadManagerApi.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);
        }

        [Fact(DisplayName = "Constructor - throws on null params")]
        public void Constructor_ThrowsOnNullParams()
        {
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(null, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(_loggerFactory.Object, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(_loggerFactory.Object, _httpClientFactory.Object, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(_loggerFactory.Object, _httpClientFactory.Object, _serviceScopeFactory.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(_loggerFactory.Object, _httpClientFactory.Object, _serviceScopeFactory.Object, _logger.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(_loggerFactory.Object, _httpClientFactory.Object, _serviceScopeFactory.Object, _logger.Object, _configuration, null, null));
            Assert.Throws<ArgumentNullException>(() => new DicomWebExportService(_loggerFactory.Object, _httpClientFactory.Object, _serviceScopeFactory.Object, _logger.Object, _configuration, _storageInfoProvider.Object, null));
        }

        [Fact(DisplayName = " ExportDataBlockCallback - Returns null if inference request cannot be found")]
        public async Task ExportDataBlockCallback_ReturnsNullIfInferenceRequestCannotBeFound()
        {
            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);

            var bytes = new byte[10];
            _random.NextBytes(bytes);

            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(bytes));
            _inferenceRequestStore.Setup(p => p.Get(It.IsAny<string>())).Returns((InferenceRequest)null);

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.DicomWeb.ExportSink, 10, It.IsAny<CancellationToken>()), Times.Once());

            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            _logger.VerifyLogging($"The specified job cannot be found in the inference request store and will not be exported.", LogLevel.Error, Times.AtLeastOnce());
            _logger.VerifyLogging($"Task {tasks.First().ExportTaskId} marked as failure and will not be retried.", LogLevel.Warning, Times.AtLeastOnce());

            await StopAndVerify(service);
        }

        [Fact(DisplayName = " ExportDataBlockCallback - Returns null if inference request doesn't include a valid DICOMweb destination")]
        public async Task ExportDataBlockCallback_ReturnsNullIfInferenceRequestContainsNoDicomWebDestination()
        {
            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

            var inferenceRequest = new InferenceRequest();

            var bytes = new byte[10];
            _random.NextBytes(bytes);

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);
            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(bytes));
            _inferenceRequestStore.Setup(p => p.Get(It.IsAny<string>())).Returns(inferenceRequest);

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.DicomWeb.ExportSink, 10, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            _logger.VerifyLogging($"The inference request contains no `outputResources` nor any DICOMweb export destinations.", LogLevel.Error, Times.AtLeastOnce());
            _logger.VerifyLogging($"Task {tasks.First().ExportTaskId} marked as failure and will not be retried.", LogLevel.Warning, Times.AtLeastOnce());

            await StopAndVerify(service);
        }

        [Fact(DisplayName = " ExportDataBlockCallback - Records STOW failures and report")]
        public async Task ExportDataBlockCallback_RecordsStowFailuresAndReportFailure()
        {
            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

            var inferenceRequest = new InferenceRequest();
            inferenceRequest.OutputResources.Add(new RequestOutputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new DicomWebConnectionDetails
                {
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                    Uri = "http://my-dicom-web.site"
                }
            });

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);

            var bytes = new byte[10];
            _random.NextBytes(bytes);

            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(bytes));
            _inferenceRequestStore.Setup(p => p.Get(It.IsAny<string>())).Returns(inferenceRequest);

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
            .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Throws(new Exception("error"));

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.DicomWeb.ExportSink, 10, It.IsAny<CancellationToken>()), Times.Once());
            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Exporting data to {inferenceRequest.OutputResources.First().ConnectionDetails.Uri}.", LogLevel.Debug, Times.AtLeastOnce());
            _logger.VerifyLogging($"Failed to export data to DICOMweb destination.", LogLevel.Error, Times.AtLeastOnce());

            await StopAndVerify(service);
        }

        [Theory(DisplayName = "Export completes entire data flow and reports status based on response StatusCode")]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.BadRequest)]
        public async Task CompletesDataflow(HttpStatusCode httpStatusCode)
        {
            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

            var url = "http://my-dicom-web.site";
            var inferenceRequest = new InferenceRequest();
            inferenceRequest.OutputResources.Add(new RequestOutputDataResource
            {
                Interface = InputInterfaceType.DicomWeb,
                ConnectionDetails = new DicomWebConnectionDetails
                {
                    AuthId = "token",
                    AuthType = ConnectionAuthType.Bearer,
                    Uri = url
                }
            });

            var tasks = ExportServiceBaseTest.GenerateTaskResponse(1);
            var bytes = new byte[10];
            _random.NextBytes(bytes);

            _workloadManagerApi.Setup(p => p.GetPendingJobs(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(tasks));
            _workloadManagerApi.Setup(p => p.ReportSuccess(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.ReportFailure(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            _workloadManagerApi.Setup(p => p.Download(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(bytes));
            _inferenceRequestStore.Setup(p => p.Get(It.IsAny<string>())).Returns(inferenceRequest);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile());

            var response = new HttpResponseMessage(httpStatusCode);
            response.Content = new StringContent("result");

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
            .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            var dataflowCompleted = new ManualResetEvent(false);
            service.ReportActionStarted += (sender, args) =>
            {
                dataflowCompleted.Set();
            };

            await service.StartAsync(_cancellationTokenSource.Token);
            dataflowCompleted.WaitOne(5000);

            _workloadManagerApi.Verify(p => p.GetPendingJobs(_configuration.Value.DicomWeb.ExportSink, 10, It.IsAny<CancellationToken>()), Times.Once());
            _workloadManagerApi.Verify(p => p.Download(tasks.First().ApplicationId, tasks.First().FileId, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Exporting data to {inferenceRequest.OutputResources.First().ConnectionDetails.Uri}.", LogLevel.Debug, Times.AtLeastOnce());

            if (httpStatusCode == HttpStatusCode.OK)
            {
                _logger.VerifyLogging($"Task marked as successful.", LogLevel.Information, Times.AtLeastOnce());
            }
            else
            {
                _logger.VerifyLogging($"Failed to export data to DICOMweb destination.", LogLevel.Error, Times.AtLeastOnce());
                _logger.VerifyLogging($"Task marked as failed.", LogLevel.Warning, Times.AtLeastOnce());
            }

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString().StartsWith($"{url}/studies/")),
               ItExpr.IsAny<CancellationToken>());

            await StopAndVerify(service);
        }

        private async Task StopAndVerify(DicomWebExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _workloadManagerApi.Invocations.Clear();
            _logger.VerifyLogging($"Export Task Watcher Hosted Service is stopping.", LogLevel.Information, Times.Once());
            Thread.Sleep(500);
            _workloadManagerApi.Verify(p => p.GetPendingJobs(TestExportService.AgentName, 10, It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}