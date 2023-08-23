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
using System.IO.Abstractions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.Events;
using Moq;
using Moq.Protected;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class DataRetrievalServiceTest
    {
        private readonly Mock<ILogger<DataRetrievalService>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<IStorageMetadataRepository> _storageMetadataWrapperRepository;
        private readonly Mock<IObjectUploadQueue> _uploadQueue;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IInferenceRequestRepository> _inferenceRequestStore;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;

        private readonly Mock<ILogger<DicomWebClient>> _loggerDicomWebClient;
        private Mock<HttpMessageHandler> _handlerMock;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public DataRetrievalServiceTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _logger = new Mock<ILogger<DataRetrievalService>>();
            _inferenceRequestStore = new Mock<IInferenceRequestRepository>();
            _loggerDicomWebClient = new Mock<ILogger<DicomWebClient>>();
            _storageMetadataWrapperRepository = new Mock<IStorageMetadataRepository>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _uploadQueue = new Mock<IObjectUploadQueue>();
            _dicomToolkit = new Mock<IDicomToolkit>();
            _fileSystem = new Mock<IFileSystem>();
            _options = Options.Create(new InformaticsGatewayConfiguration());
            _serviceScope = new Mock<IServiceScope>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns((string type) =>
            {
                return _loggerDicomWebClient.Object;
            });

            var services = new ServiceCollection();
            services.AddScoped(p => _httpClientFactory.Object);
            services.AddScoped(p => _loggerFactory.Object);
            services.AddScoped(p => _storageMetadataWrapperRepository.Object);
            services.AddScoped(p => _uploadQueue.Object);
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _dicomToolkit.Object);
            services.AddScoped(p => _inferenceRequestStore.Object);
            services.AddScoped(p => _fileSystem.Object);
            services.AddScoped(p => _storageInfoProvider.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _options.Value.Storage.TemporaryDataStorage = TemporaryDataStorageLocation.Memory;
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(true);
        }

        [RetryFact(5, 250)]
        public void GivenADataRetrievalService_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, null));

            _ = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);
        }

        [RetryFact(5, 250)]
        public async Task GivenARunningDataRetrievalService_WhenStopAsyncIsCalled_ExpectServiceToStopProcessing()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);
            Thread.Sleep(250);
            await store.StopAsync(cancellationTokenSource.Token);
            Thread.Sleep(500);

            Assert.Equal(ServiceStatus.Stopped, store.Status);
            _logger.VerifyLogging($"Data Retrieval Service is running.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Data Retrieval Service is stopping.", LogLevel.Information, Times.Once());
        }

        [RetryFact(5, 250)]
        public async Task GivenARunningDataRetrievalService_WhenStorageSpaceIsLow_ExpectNotToRetrieve()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToRetrieve).Returns(false);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);
            Thread.Sleep(250);
            await store.StopAsync(cancellationTokenSource.Token);
            Thread.Sleep(500);

            _logger.VerifyLogging($"Data Retrieval Service is running.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Data Retrieval Service is stopping.", LogLevel.Information, Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToRetrieve, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.AtLeastOnce());
        }

        [RetryFact(5, 250)]
        public async Task GivenAInferenceRequestWithFromTheDatabaseWithPendingDownloads_AtServiceStartup_ExpectToNotDownloadPreviouslyRetrievedFiles()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
                InputMetadata = new InferenceRequestMetadata()
            };
            request.InputMetadata.Details = new InferenceRequestDetails()
            {
                Type = InferenceRequestType.DicomPatientId,
                PatientId = "123"
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails()
                    {
                        Uri = "http://valid.uri/api",
                        AuthType = ConnectionAuthType.None
                    }
                });

            Assert.True(request.IsValid(out string _));

            var restoredFile = new List<FileStorageMetadata>
                {
                    new DicomFileStorageMetadata(Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),Guid.NewGuid().ToString(),DataService.DicomWeb,"calling","called"),
                    new FhirFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), FhirStorageFormat.Json, DataService.FHIR, "origin"),
                };
            _storageMetadataWrapperRepository.Setup(p => p.GetFileStorageMetdadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(restoredFile);

            _inferenceRequestStore.SetupSequence(p => p.TakeAsync(It.IsAny<CancellationToken>()))
                            .Returns(Task.FromResult(request))
                            .Returns(() =>
                            {
                                cancellationTokenSource.Cancel();
                                throw new OperationCanceledException("canceled");
                            });

            _payloadAssembler.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()));

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            foreach (var file in restoredFile)
            {
                _logger.VerifyLoggingMessageBeginsWith($"Restored previously retrieved file {file.Id}.", LogLevel.Debug, Times.Once());
            }
        }

        [RetryFact(5, 250, DisplayName = "ProcessRequest - Throws if no files found")]
        public async Task GivenAnInferenceRequest_WhenItCompletesRetrievalWithoutAnyFiles_ExpectTheRequestToBeMarkAsFailed()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>
                    {
                         new RequestedStudy
                         {
                              StudyInstanceUid = "1",
                              Series = new List<RequestedSeries>
                              {
                                  new RequestedSeries
                                  {
                                       SeriesInstanceUid = "1.1",
                                       Instances = new List<RequestedInstance>
                                       {
                                           new RequestedInstance
                                           {
                                                SopInstanceUid = new List<string>
                                                {
                                                    "1.1.2",
                                                    "1.1.3"
                                                }
                                           }
                                       }
                                  }
                              }
                         }
                    }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            _inferenceRequestStore.SetupSequence(p => p.TakeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
            .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    var content = new MultipartContent("related");
                    content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"application/dicom\""));
                    return new HttpResponseMessage() { Content = new StringContent("[]") };
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            _storageMetadataWrapperRepository.Setup(p => p.GetFileStorageMetdadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<FileStorageMetadata>());

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(2),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{url}studies/")),
               ItExpr.IsAny<CancellationToken>());

            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Never());
            _logger.VerifyLogging($"Error processing request: TransactionId = {request.TransactionId}.", LogLevel.Error, Times.Once());
        }

        [RetryFact(5, 250)]
        public async Task GivenAnInferenceRequestWithDicomUids_WhenProcessing_ExpectAllInstancesToBeRetrievedAndQueued()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomUid,
                    Studies = new List<RequestedStudy>
                    {
                         new RequestedStudy
                         {
                              StudyInstanceUid = "1",
                              Series = new List<RequestedSeries>
                              {
                                  new RequestedSeries
                                  {
                                       SeriesInstanceUid = "1.1",
                                       Instances = new List<RequestedInstance>
                                       {
                                           new RequestedInstance
                                           {
                                                SopInstanceUid = new List<string>
                                                {
                                                    "1.1.2",
                                                    "1.1.3"
                                                }
                                           }
                                       }
                                  }
                              }
                         },
                         new RequestedStudy
                         {
                              StudyInstanceUid = "2",
                              Series = new List<RequestedSeries>
                              {
                                  new RequestedSeries
                                  {
                                       SeriesInstanceUid = "2.1"
                                  }
                              }
                         },
                         new RequestedStudy
                         {
                              StudyInstanceUid = "3"
                         },
                    }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            _inferenceRequestStore.SetupSequence(p => p.TakeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock
            .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateMultipartResponse();
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            _storageMetadataWrapperRepository.Setup(p => p.GetFileStorageMetdadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<FileStorageMetadata>());

            _dicomToolkit.Setup(p => p.GetStudySeriesSopInstanceUids(It.IsAny<DicomFile>()))
                .Returns((DicomFile dicomFile) => new StudySerieSopUids
                {
                    StudyInstanceUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                    SeriesInstanceUid = dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                    SopInstanceUid = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
                });

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(4),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{url}studies/")),
               ItExpr.IsAny<CancellationToken>());

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(4));
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Exactly(4));
        }

        [RetryFact(5, 250)]
        public async Task GivenAnInferenceRequestWithPatientId_WhenProcessing_ExpectAllInstancesToBeRetrievedAndQueued()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.DicomPatientId,
                    PatientId = "ABC"
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            _inferenceRequestStore.SetupSequence(p => p.TakeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            var studyInstanceUids = new List<string>()
            {
                DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                DicomUIDGenerator.GenerateDerivedFromUUID().UID
            };
            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateQueryResult(DicomTag.PatientID, "ABC", studyInstanceUids);
                });
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => !p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateMultipartResponse();
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            _storageMetadataWrapperRepository.Setup(p => p.GetFileStorageMetdadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<FileStorageMetadata>());

            _dicomToolkit.Setup(p => p.GetStudySeriesSopInstanceUids(It.IsAny<DicomFile>()))
                .Returns((DicomFile dicomFile) => new StudySerieSopUids
                {
                    StudyInstanceUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                    SeriesInstanceUid = dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                    SopInstanceUid = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
                });

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.Query.Contains("00100020=ABC")),
               ItExpr.IsAny<CancellationToken>());

            foreach (var studyInstanceUid in studyInstanceUids)
            {
                _handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Once(),
                   ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().StartsWith($"{url}studies/{studyInstanceUid}")),
                   ItExpr.IsAny<CancellationToken>());
            }

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(studyInstanceUids.Count));
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Exactly(studyInstanceUids.Count));
        }

        [RetryFact(5, 250)]
        public async Task GivenAnInferenceRequestWithAccessionNumber_WhenProcessing_ExpectAllInstancesToBeRetrievedAndQueued()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.AccessionNumber,
                    AccessionNumber = new List<string>() { "ABC" }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Basic,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            _inferenceRequestStore.SetupSequence(p => p.TakeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            var studyInstanceUids = new List<string>()
            {
                DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                DicomUIDGenerator.GenerateDerivedFromUUID().UID
            };
            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateQueryResult(DicomTag.AccessionNumber, "ABC", studyInstanceUids);
                });
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => !p.RequestUri.Query.Contains("ABC")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return GenerateMultipartResponse();
                });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            _storageMetadataWrapperRepository.Setup(p => p.GetFileStorageMetdadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<FileStorageMetadata>());

            _dicomToolkit.Setup(p => p.GetStudySeriesSopInstanceUids(It.IsAny<DicomFile>()))
                .Returns((DicomFile dicomFile) => new StudySerieSopUids
                {
                    StudyInstanceUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                    SeriesInstanceUid = dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                    SopInstanceUid = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
                });

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.Query.Contains("00080050=ABC")),
               ItExpr.IsAny<CancellationToken>());

            foreach (var studyInstanceUid in studyInstanceUids)
            {
                _handlerMock.Protected().Verify(
                   "SendAsync",
                   Times.Once(),
                   ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().StartsWith($"{url}studies/{studyInstanceUid}")),
                   ItExpr.IsAny<CancellationToken>());
            }

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(2));
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Exactly(2));
        }

        [RetryFact(5, 250)]
        public async Task GivenAnInferenceRequestWithFhirResources_WhenProcessing_ExpectAllInstancesToBeRetrievedAndQueued()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            #region Test Data

            var url = "http://uri.test/";
            var request = new InferenceRequest
            {
                TransactionId = Guid.NewGuid().ToString(),
            };
            request.InputMetadata = new InferenceRequestMetadata
            {
                Details = new InferenceRequestDetails
                {
                    Type = InferenceRequestType.FhireResource,
                    Resources = new List<FhirResource>()
                    {
                        new FhirResource
                        {
                            Id = "1",
                            Type = "Patient"
                        }
                    }
                },
                Inputs = new List<InferenceRequestDetails>()
                {
                    new InferenceRequestDetails
                    {
                        Type = InferenceRequestType.FhireResource,
                        Resources = new List<FhirResource>()
                        {
                            new FhirResource
                            {
                                Id = "2",
                                Type = "Observation"
                            }
                        }
                    }
                }
            };
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails()
                });
            request.InputResources.Add(
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Fhir,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        AuthId = "token",
                        AuthType = ConnectionAuthType.Bearer,
                        Uri = url
                    }
                });

            Assert.True(request.IsValid(out string _));

            #endregion Test Data

            _inferenceRequestStore.SetupSequence(p => p.TakeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request))
                .Returns(() =>
                {
                    cancellationTokenSource.Cancel();
                    throw new OperationCanceledException("canceled");
                });

            _handlerMock = new Mock<HttpMessageHandler>();
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}") });

            _httpClientFactory.Setup(p => p.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(_handlerMock.Object));

            _storageMetadataWrapperRepository.Setup(p => p.GetFileStorageMetdadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<FileStorageMetadata>());

            var store = new DataRetrievalService(_logger.Object, _serviceScopeFactory.Object, _options);

            await store.StartAsync(cancellationTokenSource.Token);

            BlockUntilCancelled(cancellationTokenSource.Token);

            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.PathAndQuery.Contains("Patient/1")),
               ItExpr.IsAny<CancellationToken>());
            _handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.PathAndQuery.Contains("Observation/2")),
               ItExpr.IsAny<CancellationToken>());

            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Exactly(2));
        }

        private static HttpResponseMessage GenerateQueryResult(DicomTag dicomTag, string queryValue, List<string> studyInstanceUids)
        {
            var set = new List<string>();
            foreach (var studyInstanceUid in studyInstanceUids)
            {
                var dataset = new DicomDataset
                {
                    { dicomTag, queryValue },
                    { DicomTag.StudyInstanceUID, studyInstanceUid }
                };
                set.Add(DicomJson.ConvertDicomToJson(dataset));
            }
            var json = JsonSerializer.Serialize(set);
            var stringContent = new StringContent(json);
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = stringContent };
        }

        private static HttpResponseMessage GenerateMultipartResponse()
        {
            var data = InstanceGenerator.GenerateDicomData();
            var content = new MultipartContent("related");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("type", $"\"application/dicom\""));
            var byteContent = new StreamContent(new MemoryStream(data));
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/dicom");
            content.Add(byteContent);
            return new HttpResponseMessage() { Content = content };
        }

        private static void BlockUntilCancelled(CancellationToken token)
        {
            WaitHandle.WaitAll(new[] { token.WaitHandle });
        }
    }
}