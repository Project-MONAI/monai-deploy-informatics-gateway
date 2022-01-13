// Copyright 2022 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadNotificationServiceTest
    {
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IStorageService> _storageService;
        private readonly Mock<ILogger<PayloadNotificationService>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly Mock<IInformaticsGatewayRepository<Payload>> _payloadRepository;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PayloadNotificationServiceTest()
        {
            _fileSystem = new Mock<IFileSystem>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _storageService = new Mock<IStorageService>();
            _logger = new Mock<ILogger<PayloadNotificationService>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _payloadRepository = new Mock<IInformaticsGatewayRepository<Payload>>();
            _cancellationTokenSource = new CancellationTokenSource();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(IInformaticsGatewayRepository<Payload>)))
                .Returns(_payloadRepository.Object);

            var scope = new Mock<IServiceScope>();
            scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(scope.Object);

            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 1 };
            _options.Value.Storage.StorageServiceBucketName = "bucket";
        }

        [Fact(DisplayName = "Payload Notification Service shall stop processing when StopAsync is called")]
        public void ShallStopProcessing()
        {
            var payload = new Payload("test", 100) { State = Payload.PayloadState.Upload };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Task.Delay(100).Wait();
                    return payload;
                });

            var service = new PayloadNotificationService(_fileSystem.Object,
                                                         _payloadAssembler.Object,
                                                         _storageService.Object,
                                                         _logger.Object,
                                                         _options,
                                                         _serviceScopeFactory.Object);

            service.StartAsync(_cancellationTokenSource.Token);
            service.StopAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.CancelAfter(150);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _logger.VerifyLogging($"Stopping {service.ServiceName}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{service.ServiceName} stopped, waiting for queues to complete...", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Never());
        }

        [Fact(DisplayName = "Payload Notification Service shall restore payloads from database")]
        public void ShallRestorePayloadsFromDatabase()
        {
            var testData = new List<Payload>
            {
                new Payload("created-test", 10){ State = Payload.PayloadState.Created},
                new Payload("upload-test", 10){ State = Payload.PayloadState.Upload},
                new Payload("notification-test", 10) {State = Payload.PayloadState.Notify},
            };

            _payloadRepository.Setup(p => p.AsQueryable())
                .Returns(testData.AsQueryable())
                .Callback(() => _cancellationTokenSource.CancelAfter(500));

            var service = new PayloadNotificationService(_fileSystem.Object,
                                                         _payloadAssembler.Object,
                                                         _storageService.Object,
                                                         _logger.Object,
                                                         _options,
                                                         _serviceScopeFactory.Object);

            service.StartAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _logger.VerifyLogging("Restoring payloads from database.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"2 payloads restored from database.", LogLevel.Information, Times.Once());
        }

        [Fact(DisplayName = "Payload Notification Service shall prrocess payloads from payload assembler")]
        public void ShallProcessPayloadsFromPayloadAssembler()
        {
            var payload = new Payload("test", 100) { State = Payload.PayloadState.Upload };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(payload);

            var service = new PayloadNotificationService(_fileSystem.Object,
                                                         _payloadAssembler.Object,
                                                         _storageService.Object,
                                                         _logger.Object,
                                                         _options,
                                                         _serviceScopeFactory.Object);
            _cancellationTokenSource.CancelAfter(100);
            service.StartAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _logger.VerifyLogging($"Payload {payload.Id} added to {service.ServiceName} for processing.", LogLevel.Information, Times.AtLeastOnce());
        }

        [Fact(DisplayName = "Payload Notification Service shall upload files & retry on failure")]
        public void ShalUploadFilesAndRetryOnFailure()
        {
            _fileSystem.Setup(p => p.File.OpenRead(It.IsAny<string>())).Throws(new Exception("error"));
            _fileSystem.Setup(p => p.Path.IsPathRooted(It.IsAny<string>())).Callback((string path) => System.IO.Path.IsPathRooted(path));

            var payload = new Payload("test", 100) { State = Payload.PayloadState.Upload };
            payload.Add(new DicomFileStorageInfo("correlation", "/root", "1", "source", _fileSystem.Object) { StudyInstanceUid = "study", SeriesInstanceUid = "series", SopInstanceUid = "sop" });

            var uploadPath = Path.Combine(payload.Id.ToString(), payload.Files[0].UploadPath);
            var fileSent = false;
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) =>
                {
                    if (fileSent)
                    {
                        cancellationToken.WaitHandle.WaitOne();
                        return null;
                    }

                    fileSent = true;
                    return payload;
                });

            var service = new PayloadNotificationService(_fileSystem.Object,
                                                         _payloadAssembler.Object,
                                                         _storageService.Object,
                                                         _logger.Object,
                                                         _options,
                                                         _serviceScopeFactory.Object);
            _cancellationTokenSource.CancelAfter(1000);
            service.StartAsync(_cancellationTokenSource.Token);

            _cancellationTokenSource.Token.WaitHandle.WaitOne();
            _logger.VerifyLogging($"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Exactly(2));
            _logger.VerifyLogging($"Uploading file {uploadPath} from payload {payload.Id} to storage service.", LogLevel.Debug, Times.Exactly(2));
            _logger.VerifyLogging($"Payload {payload.Id} added back to queue for retry.", LogLevel.Warning, Times.Once());
            _logger.VerifyLogging($"Updating payload state={payload.State}, retries=1.", LogLevel.Error, Times.Once());
            _logger.VerifyLogging($"Reached maximum number of retries for payload {payload.Id}, giving up.", LogLevel.Error, Times.Once());
        }

        [Fact(DisplayName = "Payload Notification Service shall upload files & publish")]
        public void ShalUploadFilesAndPublish()
        {
            _fileSystem.Setup(p => p.File.OpenRead(It.IsAny<string>())).Returns(Stream.Null);
            _fileSystem.Setup(p => p.Path.IsPathRooted(It.IsAny<string>())).Callback((string path) => Path.IsPathRooted(path));
            _storageService.Setup(p => p.PutObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback(() => _cancellationTokenSource.CancelAfter(500));

            var payload = new Payload("test", 100) { State = Payload.PayloadState.Upload };
            payload.Add(new DicomFileStorageInfo("correlation", "/root", "1", "source", _fileSystem.Object) { StudyInstanceUid = "study", SeriesInstanceUid = "series", SopInstanceUid = "sop" });

            var uploadPath = Path.Combine(payload.Id.ToString(), payload.Files[0].UploadPath);
            var fileSent = false;
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) =>
                {
                    if (fileSent)
                    {
                        cancellationToken.WaitHandle.WaitOne();
                        return null;
                    }

                    fileSent = true;
                    return payload;
                });

            var service = new PayloadNotificationService(_fileSystem.Object,
                                                         _payloadAssembler.Object,
                                                         _storageService.Object,
                                                         _logger.Object,
                                                         _options,
                                                         _serviceScopeFactory.Object);
            service.StartAsync(_cancellationTokenSource.Token);

            _cancellationTokenSource.Token.WaitHandle.WaitOne();
            _logger.VerifyLogging($"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Uploading file {uploadPath} from payload {payload.Id} to storage service.", LogLevel.Debug, Times.Once());
            _logger.VerifyLogging($"Payload {payload.Id} ready to be published.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Payload {payload.Id} information published.", LogLevel.Information, Times.Never());

            _storageService.Verify(p => p.PutObject(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
