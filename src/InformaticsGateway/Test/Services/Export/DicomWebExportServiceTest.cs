/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Storage;
using Monai.Deploy.Storage.API;
using Moq;
using Moq.Protected;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Export
{
    public class DicomWebExportServiceTest
    {
        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<IMessageBrokerSubscriberService> _messageSubscriberService;
        private readonly Mock<IMessageBrokerPublisherService> _messagePublisherService;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly Mock<IInferenceRequestRepository> _inferenceRequestStore;
        private readonly Mock<ILogger<DicomWebExportService>> _logger;
        private readonly Mock<ILogger<DicomWebClient>> _loggerDicomWebClient;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Mock<HttpMessageHandler> _handlerMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IDicomToolkit> _dicomToolkit;

        public DicomWebExportServiceTest()
        {
            _storageService = new Mock<IStorageService>();
            _messageSubscriberService = new Mock<IMessageBrokerSubscriberService>();
            _messagePublisherService = new Mock<IMessageBrokerPublisherService>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _inferenceRequestStore = new Mock<IInferenceRequestRepository>();
            _logger = new Mock<ILogger<DicomWebExportService>>();
            _loggerDicomWebClient = new Mock<ILogger<DicomWebClient>>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);
            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _dicomToolkit = new Mock<IDicomToolkit>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInferenceRequestRepository)))
                .Returns(_inferenceRequestStore.Object);
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

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_loggerDicomWebClient.Object);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "Constructor - throws on null params")]
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

        [RetryFact(5, 250, DisplayName = "ExportDataBlockCallback - Returns null if inference request cannot be found")]
        public async Task ExportDataBlockCallback_ReturnsNullIfInferenceRequestCannotBeFound()
        {
            var transactionId = Guid.NewGuid().ToString();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Action<MessageReceivedEventArgs>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs(transactionId));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _inferenceRequestStore.Setup(p => p.GetInferenceRequest(It.IsAny<string>())).Returns((InferenceRequest)null);

            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging($"The specified inference request '{transactionId}' cannot be found and will not be exported.", LogLevel.Error, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "ExportDataBlockCallback - Returns null if inference request doesn't include a valid DICOMweb destination")]
        public async Task ExportDataBlockCallback_ReturnsNullIfInferenceRequestContainsNoDicomWebDestination()
        {
            var transactionId = Guid.NewGuid().ToString();
            var inferenceRequest = new InferenceRequest();
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Action<MessageReceivedEventArgs>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs(transactionId));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _inferenceRequestStore.Setup(p => p.GetInferenceRequest(It.IsAny<string>())).Returns(inferenceRequest);

            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging($"The inference request contains no `outputResources` nor any DICOMweb export destinations.", LogLevel.Error, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "ExportDataBlockCallback - Records STOW failures and report")]
        public async Task ExportDataBlockCallback_RecordsStowFailuresAndReportFailure()
        {
            var transactionId = Guid.NewGuid().ToString();
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
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

            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Action<MessageReceivedEventArgs>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs(transactionId));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _inferenceRequestStore.Setup(p => p.GetInferenceRequest(It.IsAny<string>())).Returns(inferenceRequest);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile(sopInstanceUid: sopInstanceUid));

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

            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == ExportStatus.Failure)), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging($"Exporting data to {inferenceRequest.OutputResources.First().ConnectionDetails.Uri}.", LogLevel.Debug, Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Failed to store DICOM instances", LogLevel.Error, Times.Once());
        }

        [RetryTheory(DisplayName = "Export completes entire data flow and reports status based on response StatusCode")]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.BadRequest)]
        public async Task CompletesDataflow(HttpStatusCode httpStatusCode)
        {
            var url = "http://my-dicom-web.site";
            var transactionId = Guid.NewGuid().ToString();
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

            _storageInfoProvider.Setup(p => p.HasSpaceAvailableForExport).Returns(true);

            _messagePublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()));
            _messageSubscriberService.Setup(p => p.Acknowledge(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(p => p.RequeueWithDelay(It.IsAny<MessageBase>()));
            _messageSubscriberService.Setup(
                p => p.Subscribe(It.IsAny<string>(),
                                 It.IsAny<string>(),
                                 It.IsAny<Action<MessageReceivedEventArgs>>(),
                                 It.IsAny<ushort>()))
                .Callback<string, string, Action<MessageReceivedEventArgs>, ushort>((topic, queue, messageReceivedCallback, prefetchCount) =>
                {
                    messageReceivedCallback(CreateMessageReceivedEventArgs(transactionId));
                });

            _storageService.Setup(p => p.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")));

            _inferenceRequestStore.Setup(p => p.GetInferenceRequest(It.IsAny<string>())).Returns(inferenceRequest);
            _dicomToolkit.Setup(p => p.Load(It.IsAny<byte[]>())).Returns(InstanceGenerator.GenerateDicomFile());

            var response = new HttpResponseMessage(httpStatusCode)
            {
                Content = new StringContent("result")
            };

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

            var service = new DicomWebExportService(
                _loggerFactory.Object,
                _httpClientFactory.Object,
                _serviceScopeFactory.Object,
                _logger.Object,
                _configuration,
                _storageInfoProvider.Object,
                _dicomToolkit.Object);

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
                               It.Is<Message>(match => (match.ConvertTo<ExportCompleteEvent>()).Status == (httpStatusCode == HttpStatusCode.OK ? ExportStatus.Success : ExportStatus.Failure))), Times.Once());
            _messageSubscriberService.Verify(p => p.Acknowledge(It.IsAny<MessageBase>()), Times.Once());
            _messageSubscriberService.Verify(p => p.RequeueWithDelay(It.IsAny<MessageBase>()), Times.Never());
            _messageSubscriberService.Verify(p => p.Subscribe(It.IsAny<string>(),
                                                              It.IsAny<string>(),
                                                              It.IsAny<Action<MessageReceivedEventArgs>>(),
                                                              It.IsAny<ushort>()), Times.Once());

            _logger.VerifyLogging($"Exporting data to {inferenceRequest.OutputResources.First().ConnectionDetails.Uri}.", LogLevel.Debug, Times.AtLeastOnce());

            if (httpStatusCode == HttpStatusCode.OK)
            {
                _logger.VerifyLogging($"All data exported successfully.", LogLevel.Information, Times.Once());
            }
            else
            {
                _logger.VerifyLogging($"Failed to export to destination.", LogLevel.Error, Times.Once());
            }

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.AtLeastOnce(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString().StartsWith($"{url}/studies/")),
               ItExpr.IsAny<CancellationToken>());
        }

        private static MessageReceivedEventArgs CreateMessageReceivedEventArgs(string transactionId)
        {
            var exportRequestEvent = new ExportRequestEvent
            {
                ExportTaskId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                Destinations = new[] { transactionId },
                Files = new[] { "file1" },
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };
            var jsonMessage = new JsonMessage<ExportRequestEvent>(exportRequestEvent, MessageBrokerConfiguration.InformaticsGatewayApplicationId, exportRequestEvent.CorrelationId, exportRequestEvent.DeliveryTag);

            return new MessageReceivedEventArgs(jsonMessage.ToMessage(), CancellationToken.None);
        }

        private async Task StopAndVerify(DicomWebExportService service)
        {
            await service.StopAsync(_cancellationTokenSource.Token);
            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
            Thread.Sleep(500);
        }
    }
}
