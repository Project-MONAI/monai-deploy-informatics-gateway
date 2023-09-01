/*
 * Copyright 2023 MONAI Consortium
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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Test.PlugIns;
using Monai.Deploy.Messaging.Events;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class ApplicationEntityHandlerTest
    {
        private readonly Mock<ILogger<ApplicationEntityHandler>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IInputDataPlugInEngine> _inputDataPlugInEngine;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IObjectUploadQueue> _uploadQueue;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IFileSystem _fileSystem;
        private readonly IServiceProvider _serviceProvider;

        public ApplicationEntityHandlerTest()
        {
            _logger = new Mock<ILogger<ApplicationEntityHandler>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _inputDataPlugInEngine = new Mock<IInputDataPlugInEngine>();

            _payloadAssembler = new Mock<IPayloadAssembler>();
            _uploadQueue = new Mock<IObjectUploadQueue>();
            _options = Options.Create<InformaticsGatewayConfiguration>(new InformaticsGatewayConfiguration());
            _fileSystem = new MockFileSystem();

            var services = new ServiceCollection();
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _uploadQueue.Object);
            services.AddScoped(p => _fileSystem);
            services.AddScoped(p => _inputDataPlugInEngine.Object);

            _inputDataPlugInEngine.Setup(p => p.Configure(It.IsAny<IReadOnlyList<string>>()));

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _options.Value.Storage.TemporaryDataStorage = TemporaryDataStorageLocation.Memory;
        }

        [RetryFact(5, 250)]
        public void GivenAApplicationEntityHandler_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationEntityHandler(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ApplicationEntityHandler(_serviceScopeFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, null));

            _ = new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, _options);
        }

        [RetryFact(5, 250)]
        public async Task GivenAApplicationEntityHandler_WhenHandleInstanceAsyncIsCalledWithoutConfigured_ExpectToThrowException()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "TESTAET",
                Name = "TESTAET",
                Workflows = new List<string>() { "AppA", "AppB", Guid.NewGuid().ToString() },
                IgnoredSopClasses = new List<string> { DicomUID.SecondaryCaptureImageStorage.UID }
            };

            var handler = new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, _options);

            var request = GenerateRequest();
            var dicomToolkit = new DicomToolkit();
            var uids = dicomToolkit.GetStudySeriesSopInstanceUids(request.File);

            await Assert.ThrowsAsync<NotSupportedException>(async () => await handler.HandleInstanceAsync(request, aet.AeTitle, "CALLING", Guid.NewGuid(), uids));
        }

        [RetryFact(5, 250)]
        public async Task GivenACStoreRequest_WhenTheSopClassIsInTheIgnoreList_ExpectInstanceNotBeQueued()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "TESTAET",
                Name = "TESTAET",
                Workflows = new List<string>() { "AppA", "AppB", Guid.NewGuid().ToString() },
                IgnoredSopClasses = new List<string> { DicomUID.SecondaryCaptureImageStorage.UID }
            };

            var handler = new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, _options);
            handler.Configure(aet, Configuration.DicomJsonOptions.Complete, true);

            var request = GenerateRequest();
            var dicomToolkit = new DicomToolkit();
            var uids = dicomToolkit.GetStudySeriesSopInstanceUids(request.File);

            await handler.HandleInstanceAsync(request, aet.AeTitle, "CALLING", Guid.NewGuid(), uids);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>(), It.IsAny<uint>()), Times.Never());
        }

        [RetryFact(5, 250)]
        public async Task GivenACStoreRequest_WhenTheSopClassIsNotInTheAllowedList_ExpectInstanceNotBeQueued()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "TESTAET",
                Name = "TESTAET",
                Workflows = new List<string>() { "AppA", "AppB", Guid.NewGuid().ToString() },
                AllowedSopClasses = new List<string> { DicomUID.UltrasoundImageStorage.UID }
            };

            var handler = new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, _options);
            handler.Configure(aet, Configuration.DicomJsonOptions.Complete, true);

            var request = GenerateRequest();
            var dicomToolkit = new DicomToolkit();
            var uids = dicomToolkit.GetStudySeriesSopInstanceUids(request.File);

            await handler.HandleInstanceAsync(request, aet.AeTitle, "CALLING", Guid.NewGuid(), uids);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>(), It.IsAny<uint>()), Times.Never());
        }

        [RetryFact(5, 250)]
        public async Task GivenACStoreRequest_WhenHandleInstanceAsyncIsCalled_ExpectADicomFileStorageMetadataToBeCreatedAndQueued()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "TESTAET",
                Name = "TESTAET",
                Workflows = new List<string>() { "AppA", "AppB", Guid.NewGuid().ToString() },
                PlugInAssemblies = new List<string>() { typeof(TestInputDataPlugInAddWorkflow).AssemblyQualifiedName }
            };

            var handler = new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, _options);
            handler.Configure(aet, Configuration.DicomJsonOptions.Complete, true);

            var request = GenerateRequest();
            var dicomToolkit = new DicomToolkit();
            var uids = dicomToolkit.GetStudySeriesSopInstanceUids(request.File);
            _inputDataPlugInEngine.Setup(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()))
                .Returns((DicomFile dicomFile, FileStorageMetadata fileMetadata) => Task.FromResult(new Tuple<DicomFile, FileStorageMetadata>(dicomFile, fileMetadata)));

            await handler.HandleInstanceAsync(request, aet.AeTitle, "CALLING", Guid.NewGuid(), uids);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Once());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>(), It.IsAny<uint>()), Times.Once());
            _inputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Once());
            _inputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()), Times.Once());
        }

        [RetryFact(5, 250)]
        public void GivenAConfiguredAETitle_WhenConfiguringAgainWithDifferentAETitle_ExpectAnExceptionToBeThrown()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "TESTAET",
                Name = "TESTAET",
                Workflows = new List<string>() { "AppA", "AppB", Guid.NewGuid().ToString() }
            };

            var newAet = new MonaiApplicationEntity()
            {
                AeTitle = "TESTAET",
                Name = "TESTAET",
                Workflows = new List<string>() { "AppA", "AppB", Guid.NewGuid().ToString() }
            };
            var handler = new ApplicationEntityHandler(_serviceScopeFactory.Object, _logger.Object, _options);
            handler.Configure(aet, Configuration.DicomJsonOptions.Complete, true);

            newAet.AeTitle = "NewAETitle";
            Assert.Throws<InvalidOperationException>(() => handler.Configure(newAet, Configuration.DicomJsonOptions.Complete, true));

            newAet.AeTitle = "TESTAET";
            newAet.Name = "NewName";
            Assert.Throws<InvalidOperationException>(() => handler.Configure(newAet, Configuration.DicomJsonOptions.Complete, true));
        }

        private static DicomCStoreRequest GenerateRequest()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var file = new DicomFile(dataset);
            return new DicomCStoreRequest(file);
        }
    }
}