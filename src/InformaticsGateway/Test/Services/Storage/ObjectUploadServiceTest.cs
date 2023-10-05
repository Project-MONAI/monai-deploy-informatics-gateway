/*
 * Copyright 2022-2023 MONAI Consortium
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
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Storage.API;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Storage
{
    public class ObjectUploadServiceTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<ObjectUploadService>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<ILogger<ObjectUploadQueue>> _uploadQueueLogger;
        private readonly IObjectUploadQueue _uploadQueue;
        private readonly Mock<IStorageService> _storageService;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ServiceProvider _serviceProvider;
        private readonly Mock<IServiceScope> _serviceScope;

        public ObjectUploadServiceTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _uploadQueueLogger = new Mock<ILogger<ObjectUploadQueue>>();
            _uploadQueue = new ObjectUploadQueue(_uploadQueueLogger.Object);
            _storageService = new Mock<IStorageService>();
            _logger = new Mock<ILogger<ObjectUploadService>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScope = new Mock<IServiceScope>();

            var services = new ServiceCollection();
            services.AddScoped(p => _uploadQueue);
            services.AddScoped(p => _storageService.Object);
            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _options.Value.Storage.TemporaryStorageBucket = "bucket";
            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 1 };

            _storageService.Setup(p => p.VerifyObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        [RetryFact(10, 250)]
        public void GivenAObjectUploadService_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectUploadService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, null));

            _ = new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, _options);
        }

        [RetryFact(10, 250)]
        public void GivenAObjectUploadService_WhenStartAsyncIsCalled_ExpectServiceStatusToBeSet()
        {
            var svc = new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);
        }

        [RetryFact(10, 250)]
        public void GivenAObjectUploadService_WhenInitialized_ExpectItToRemovingAllPendingObjects()
        {
            var svc = new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, _options);
            Assert.NotNull(svc);
        }

        [RetryFact(10, 250)]
        public async Task GivenADicomFileStorageMetadata_WhenQueuedForUpload_ExpectTwoFilesToBeUploaded()
        {
            var countdownEvent = new CountdownEvent(2);
            _storageService.Setup(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    countdownEvent.Signal();
                });
            var svc = new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var file = await GenerateDicomFileStorageMetadata();
            _uploadQueue.Queue(file);

            Assert.True(countdownEvent.Wait(TimeSpan.FromSeconds(3)));

            _storageService.Verify(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [RetryFact(10, 250)]
        public async Task GivenADicomFileStorageMetadata_WhenVerificationFailsOver3Times_ExpectExceptionToBeThrow()
        {
            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 1 };
            var countdownEvent = new CountdownEvent(3);
            _storageService.Setup(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));
            _storageService.Setup(p => p.VerifyObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    countdownEvent.Signal();
                })
                .ReturnsAsync(false);

            var svc = new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var file = await GenerateDicomFileStorageMetadata();
            _uploadQueue.Queue(file);

            Assert.True(countdownEvent.Wait(TimeSpan.FromSeconds(3)));

            _storageService.Verify(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once());
            _storageService.Verify(p => p.VerifyObjectExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _logger.VerifyLoggingMessageBeginsWith("Failed to upload file", LogLevel.Warning, Times.Once());
        }

        [RetryFact(10, 25000)]
        public async Task GivenAFhirFileStorageMetadata_WhenQueuedForUpload_ExpectSingleFileToBeUploaded()
        {
            var countdownEvent = new CountdownEvent(1);
            _storageService.Setup(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    countdownEvent.Signal();
                });
            var svc = new ObjectUploadService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var file = await GenerateFhirFileStorageMetadata();
            _uploadQueue.Queue(file);

            Assert.True(countdownEvent.Wait(TimeSpan.FromSeconds(3)));

            _storageService.Verify(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        private async Task<FhirFileStorageMetadata> GenerateFhirFileStorageMetadata()
        {
            var file = new FhirFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), FhirStorageFormat.Json, Messaging.Events.DataService.FHIR, "origin");

            await file.SetDataStream("[]", TemporaryDataStorageLocation.Memory);
            file.PayloadId = Guid.NewGuid().ToString();
            return file;
        }

        private async Task<DicomFileStorageMetadata> GenerateDicomFileStorageMetadata()
        {
            var file = new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Messaging.Events.DataService.DIMSE, "SOURCE", "AET");
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var dicomFile = new DicomFile(dataset);
            await file.SetDataStreams(dicomFile, "[]", TemporaryDataStorageLocation.Memory);
            file.PayloadId = Guid.NewGuid().ToString();
            return file;
        }
    }
}
