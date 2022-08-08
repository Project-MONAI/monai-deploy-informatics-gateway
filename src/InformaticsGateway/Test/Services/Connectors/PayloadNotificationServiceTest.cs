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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.API;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Connectors
{
    public class PayloadNotificationServiceTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<PayloadNotificationService>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IMessageBrokerPublisherService> _messageBrokerPublisherService;
        private readonly Mock<IPayloadNotificationActionHandler> _payloadNotificationActionHandler;
        private readonly Mock<IPayloadMoveActionHandler> _payloadMoveActionHandler;
        private readonly Mock<IInformaticsGatewayRepository<Payload>> _payloadRepository;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public PayloadNotificationServiceTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _logger = new Mock<ILogger<PayloadNotificationService>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _payloadAssembler = new Mock<IPayloadAssembler>();
            _messageBrokerPublisherService = new Mock<IMessageBrokerPublisherService>();
            _payloadNotificationActionHandler = new Mock<IPayloadNotificationActionHandler>();
            _payloadMoveActionHandler = new Mock<IPayloadMoveActionHandler>();
            _payloadRepository = new Mock<IInformaticsGatewayRepository<Payload>>();

            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScope = new Mock<IServiceScope>();

            var services = new ServiceCollection();
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _messageBrokerPublisherService.Object);
            services.AddScoped(p => _payloadNotificationActionHandler.Object);
            services.AddScoped(p => _payloadMoveActionHandler.Object);
            services.AddScoped(p => _payloadRepository.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _options.Value.Database.Retries.DelaysMilliseconds = new[] { 1 };
            _options.Value.Storage.Retries.DelaysMilliseconds = new[] { 1 };
            _options.Value.Storage.StorageServiceBucketName = "bucket";
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact]
        public void GivenAPayloadNotificationService_AtInitialization_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationService(_serviceScopeFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, null));
        }

        [RetryFact]
        public async Task GivenThePayloadNotificationService_WhenStopAsyncIsCalled_ExpectServiceToStopAnyProcessing()
        {
            var payload = new Payload("test", Guid.NewGuid().ToString(), 100) { State = Payload.PayloadState.Move };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Task.Delay(100).Wait();
                    return payload;
                });

            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            await service.StartAsync(_cancellationTokenSource.Token);
            await service.StopAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.CancelAfter(150);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _logger.VerifyLogging($"{service.ServiceName} is stopping.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Waiting for {service.ServiceName} to stop.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Never());
        }

        [RetryFact]
        public void GivenPayloadsStoredInTheDatabase_WhenServiceStarts_ExpectThePayloadsToBeRestored()
        {
            var testData = new List<Payload>
            {
                new Payload("created-test", Guid.NewGuid().ToString(), 10){ State = Payload.PayloadState.Created},
                new Payload("upload-test", Guid.NewGuid().ToString(), 10){ State = Payload.PayloadState.Move},
                new Payload("notification-test", Guid.NewGuid().ToString(), 10) {State = Payload.PayloadState.Notify},
            };

            _payloadRepository.Setup(p => p.AsQueryable())
                .Returns(testData.AsQueryable())
                .Callback(() => _cancellationTokenSource.CancelAfter(500));

            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            service.StartAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _payloadMoveActionHandler.Verify(p => p.MoveFilesAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
            _payloadNotificationActionHandler.Verify(p => p.NotifyAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        }

        [RetryFact]
        public void GivenAPayload_WhenDequedFromPayloadAssemblerAndFailedToBeProcessByTheMoveActionHandler()
        {
            var resetEvent = new ManualResetEventSlim();
            var payload = new Payload("test", Guid.NewGuid().ToString(), 100) { State = Payload.PayloadState.Move };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(payload);

            _payloadMoveActionHandler.Setup(p => p.MoveFilesAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()))
                .Callback(() => resetEvent.Set())
                .Throws(new PayloadNotifyException(PayloadNotifyException.FailureReason.IncorrectState));

            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            service.StartAsync(_cancellationTokenSource.Token);
            resetEvent.Wait();
        }

        [RetryFact]
        public void GivenAPayload_WhenDequedFromPayloadAssembler_ExpectThePayloadBeProcessedByTheMoveActionHandler()
        {
            var payload = new Payload("test", Guid.NewGuid().ToString(), 100) { State = Payload.PayloadState.Move };
            _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
                .Returns(payload);

            var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

            _cancellationTokenSource.CancelAfter(100);
            service.StartAsync(_cancellationTokenSource.Token);
            _cancellationTokenSource.Token.WaitHandle.WaitOne();

            _payloadMoveActionHandler.Verify(p => p.MoveFilesAsync(It.IsAny<Payload>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<ActionBlock<Payload>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());

            _logger.VerifyLogging($"Payload {payload.Id} added to {service.ServiceName} for processing.", LogLevel.Information, Times.AtLeastOnce());
        }

        //[RetryFact(DisplayName = "Payload Notification Service shall upload files & retry on failure")]
        //public void PayloadNotificationService_ShalUploadFilesAndRetryOnFailure()
        //{
        //    _fileSystem.Setup(p => p.File.OpenRead(It.IsAny<string>())).Throws(new Exception("error"));
        //    _fileSystem.Setup(p => p.Path.IsPathRooted(It.IsAny<string>())).Callback((string path) => System.IO.Path.IsPathRooted(path));

        //    var fileInfo = new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "study", "series", "sop")
        //    {
        //        Source = "RAET",
        //        CalledAeTitle = "AET"
        //    };

        //    var payload = new Payload("test", Guid.NewGuid().ToString(), 100) { State = Payload.PayloadState.Upload };
        //    payload.Add(fileInfo);

        //    var fileSent = false;
        //    _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
        //        .Returns((CancellationToken cancellationToken) =>
        //        {
        //            if (fileSent)
        //            {
        //                cancellationToken.WaitHandle.WaitOne();
        //                return null;
        //            }

        //            fileSent = true;
        //            _cancellationTokenSource.CancelAfter(1000);
        //            return payload;
        //        });

        //    var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

        //    service.StartAsync(_cancellationTokenSource.Token);

        //    _cancellationTokenSource.Token.WaitHandle.WaitOne();
        //    _logger.VerifyLogging($"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Exactly(2));
        //    _logger.VerifyLogging($"Failed to upload payload {payload.Id}; added back to queue for retry.", LogLevel.Warning, Times.Once());
        //    _logger.VerifyLogging($"Updating payload {payload.Id} state={payload.State}, retries=1.", LogLevel.Error, Times.Once());
        //    _logger.VerifyLogging($"Reached maximum number of retries for payload {payload.Id}, giving up.", LogLevel.Error, Times.Once());

        //    _logger.VerifyLoggingMessageBeginsWith($"Uploading file ", LogLevel.Debug, Times.Exactly(2));
        //    _instanceCleanupQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
        //}

        //[RetryFact(DisplayName = "Payload Notification Service shall publish workflow request & retry on failure")]
        //public void PayloadNotificationService_ShallPublishAndRetryOnFailure()
        //{
        //    _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
        //       .Callback(() => _cancellationTokenSource.Token.WaitHandle.WaitOne());
        //    _messageBrokerPublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>())).Throws(new Exception("error"));

        //    var payload = new Payload("test", Guid.NewGuid().ToString(), 100) { State = Payload.PayloadState.Notify };
        //    var metadata = new DicomFileStorageMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "study", "series", "sop")
        //    {
        //        Source = "RAET",
        //        CalledAeTitle = "AET"
        //    };
        //    metadata.SetWorkflows("workflow1", "workflow2");
        //    payload.Add(metadata);

        //    _payloadRepository.Setup(p => p.AsQueryable())
        //        .Returns((new List<Payload> { payload }).AsQueryable())
        //        .Callback(() => _cancellationTokenSource.CancelAfter(500));

        //    var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

        //    _cancellationTokenSource.CancelAfter(1000);
        //    service.StartAsync(_cancellationTokenSource.Token);

        //    _cancellationTokenSource.Token.WaitHandle.WaitOne();
        //    _logger.VerifyLogging($"Generating workflow request message for payload {payload.Id}...", LogLevel.Debug, Times.Exactly(2));
        //    _logger.VerifyLoggingMessageBeginsWith($"Publishing workflow request message ID=", LogLevel.Information, Times.Exactly(2));
        //    _logger.VerifyLoggingMessageBeginsWith($"Workflow request published, ID=", LogLevel.Information, Times.Never());
        //    _logger.VerifyLogging($"Failed to publish workflow request for payload {payload.Id}; added back to queue for retry.", LogLevel.Warning, Times.Once());
        //    _logger.VerifyLogging($"Updating payload {payload.Id} state={payload.State}, retries=1.", LogLevel.Error, Times.Once());
        //    _logger.VerifyLogging($"Reached maximum number of retries for payload {payload.Id}, giving up.", LogLevel.Error, Times.Once());
        //}

        //[RetryFact(DisplayName = "Payload Notification Service shall upload files & publish")]
        //public void PayloadNotificationService_ShalUploadFilesAndPublish()
        //{
        //    _fileSystem.Setup(p => p.File.OpenRead(It.IsAny<string>())).Returns(Stream.Null);
        //    _fileSystem.Setup(p => p.Path.IsPathRooted(It.IsAny<string>())).Callback((string path) => Path.IsPathRooted(path));
        //    _storageService.Setup(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()));

        //    _messageBrokerPublisherService.Setup(p => p.Publish(It.IsAny<string>(), It.IsAny<Message>()))
        //        .Callback(() => _cancellationTokenSource.CancelAfter(500));

        //    var fileInfo = new DicomFileStorageInfo(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "study", "series", "sop")
        //    {
        //        Source = "RAET",
        //        CalledAeTitle = "AET"
        //    };
        //    fileInfo.SetWorkflows("workflow1", "workflow2");
        //    var uploadPath = Path.Combine("study", "series", "instance.dcm");
        //    var payload = new Payload("test", Guid.NewGuid().ToString(), 100) { State = Payload.PayloadState.Upload };
        //    payload.Add(fileInfo);

        //    var filePath = payload.Files[0].FilePath;

        //    var fileSent = false;
        //    _payloadAssembler.Setup(p => p.Dequeue(It.IsAny<CancellationToken>()))
        //        .Returns((CancellationToken cancellationToken) =>
        //        {
        //            if (fileSent)
        //            {
        //                cancellationToken.WaitHandle.WaitOne();
        //                return null;
        //            }

        //            fileSent = true;
        //            return payload;
        //        });

        //    var service = new PayloadNotificationService(_serviceScopeFactory.Object, _logger.Object, _options);

        //    service.StartAsync(_cancellationTokenSource.Token);

        //    _cancellationTokenSource.Token.WaitHandle.WaitOne();
        //    _logger.VerifyLogging($"Uploading payload {payload.Id} to storage service at {_options.Value.Storage.StorageServiceBucketName}.", LogLevel.Information, Times.Once());
        //    _logger.VerifyLogging($"Uploading file {filePath} from payload {payload.Id} to storage service.", LogLevel.Debug, Times.Once());
        //    _logger.VerifyLogging($"Payload {payload.Id} ready to be published.", LogLevel.Information, Times.Once());
        //    _logger.VerifyLoggingMessageBeginsWith($"Workflow request published to {_options.Value.Messaging.Topics.WorkflowRequest}, message ID=", LogLevel.Information, Times.Once());

        //    _storageService.Verify(p => p.PutObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        //    _instanceCleanupQueue.Verify(p => p.Queue(It.IsAny<FileStorageInfo>()), Times.Once());

        //    _messageBrokerPublisherService.Verify(
        //        p => p.Publish(
        //            It.Is<string>(p => p.Equals(_options.Value.Messaging.Topics.WorkflowRequest)),
        //            It.Is<Message>(p => VerifyHelper(payload, p)))
        //        , Times.Once());
        //}

        private bool VerifyHelper(Payload payload, Message message)
        {
            var workflowRequestEvent = message.ConvertTo<WorkflowRequestEvent>();
            if (workflowRequestEvent is null) return false;
            if (workflowRequestEvent.Payload.Count != 1) return false;
            if (workflowRequestEvent.PayloadId != payload.Id) return false;
            if (workflowRequestEvent.FileCount != payload.Files.Count) return false;
            if (workflowRequestEvent.CorrelationId != payload.CorrelationId) return false;
            if (workflowRequestEvent.Timestamp != payload.DateTimeCreated) return false;
            if (workflowRequestEvent.CallingAeTitle != payload.Files.First().Source) return false;
            if (workflowRequestEvent.CalledAeTitle != payload.Files.OfType<DicomFileStorageMetadata>().First().CalledAeTitle) return false;

            var workflowInPayload = payload.GetWorkflows();
            if (workflowRequestEvent.Workflows.Count() != workflowInPayload.Count) return false;
            if (workflowRequestEvent.Workflows.Except(workflowInPayload).Any()) return false;
            if (workflowInPayload.Except(workflowRequestEvent.Workflows).Any()) return false;

            return true;
        }
    }
}
