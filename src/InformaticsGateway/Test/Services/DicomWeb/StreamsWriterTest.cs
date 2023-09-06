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
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.Events;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.DicomWeb
{
    public class StreamsWriterTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<StreamsWriter>> _logger;
        private readonly MockFileSystem _fileSystem;
        private readonly Mock<IObjectUploadQueue> _uploadQueue;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IInputDataPlugInEngine> _inputDataPlugInEngine;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IServiceProvider _serviceProvider;

        public StreamsWriterTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _uploadQueue = new Mock<IObjectUploadQueue>();
            _dicomToolkit = new Mock<IDicomToolkit>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _logger = new Mock<ILogger<StreamsWriter>>();
            _inputDataPlugInEngine = new Mock<IInputDataPlugInEngine>();
            _fileSystem = new MockFileSystem();
            _configuration.Value.Storage.LocalTemporaryStoragePath = "./temp";

            var services = new ServiceCollection();
            services.AddScoped(p => _inputDataPlugInEngine.Object);

            _serviceProvider = services.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.SetupGet(p => p.ServiceProvider).Returns(_serviceProvider);

            _inputDataPlugInEngine.Setup(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()))
                .Returns((DicomFile dicomFile, FileStorageMetadata fileMetadata) => Task.FromResult(new Tuple<DicomFile, FileStorageMetadata>(dicomFile, fileMetadata)));
        }

        [Fact]
        public void GivenAStreamsWriter_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(null, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_serviceScopeFactory.Object, null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, null));
            var exception = Record.Exception(() => new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem));

            Assert.Null(exception);
        }

        [Fact]
        public async Task GivenAHttpStream_WhenFailedToOpenAsDicomInstance_ExpectStatus409ToBeReturned()
        {
            _dicomToolkit.Setup(p => p.OpenAsync(It.IsAny<Stream>(), It.IsAny<FileReadOption>()))
                .Throws(new Exception("error"));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                null,
                "Workflow",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Fact]
        public async Task GivingADicomInstanceWithoutAMatchingStudyInstanceUid_WhenSaveingInstance_ExpectAWarningAndNotStored()
        {
            var uids = new List<StudySeriesSopAids>();
            SetupDicomToolkitMocks(uids);

            _payloadAssembler.Setup(p => p.QueueAsync(It.IsAny<string>(),
                It.IsAny<DicomFileStorageMetadata>(),
                It.IsAny<DataOrigin>(),
                It.IsAny<uint>(), null));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var correlationId = Guid.NewGuid().ToString();
            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                null,
                "Workflow",
                correlationId,
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);

            var referencedSopSequence = result.Data.GetSequence(DicomTag.FailedSOPSequence);
            Assert.Equal(streams.Count, referencedSopSequence.Items.Count);

            foreach (var item in referencedSopSequence.Items)
            {
                var uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.Contains(uids, p => p.SopInstanceUid == uid);
                uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                Assert.Contains(uids, p => p.SopClassUid == uid);
                var warningReason = item.GetSingleValue<ushort>(DicomTag.FailureReason);
                Assert.Equal(DicomStatus.StorageDataSetDoesNotMatchSOPClassWarning.Code, warningReason);
            }

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.QueueAsync(It.Is<string>(p => p == correlationId),
                    It.IsAny<DicomFileStorageMetadata>(),
                    It.IsAny<DataOrigin>(),
                    It.IsAny<uint>(), null),
                Times.Never());
        }

        [Fact]
        public async Task GivingAnEmptyRequest_WhenSaveingInstance_ExpectFailures()
        {
            var uids = new List<StudySeriesSopAids>();
            SetupDicomToolkitMocks(uids);
            _payloadAssembler.Setup(p => p.QueueAsync(It.IsAny<string>(),
                It.IsAny<DicomFileStorageMetadata>(),
                It.IsAny<DataOrigin>(),
                It.IsAny<uint>(), null));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var correlationId = Guid.NewGuid().ToString();
            var streams = new List<Stream>();
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                null,
                "Workflow",
                correlationId,
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.QueueAsync(It.Is<string>(p => p == correlationId),
                    It.IsAny<DicomFileStorageMetadata>(),
                    It.IsAny<DataOrigin>(),
                    It.IsAny<uint>(), null),
                Times.Never());
            _inputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Never());
            _inputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()), Times.Never());
        }

        [Fact]
        public async Task GivingAValidDicomInstanceWithZeroLength_WhenSaveingInstance_ExpectFailures()
        {
            var uids = new List<StudySeriesSopAids>();
            SetupDicomToolkitMocks(uids);
            _payloadAssembler.Setup(p => p.QueueAsync(It.IsAny<string>(),
                It.IsAny<DicomFileStorageMetadata>(),
                It.IsAny<DataOrigin>(),
                It.IsAny<uint>(), null));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var correlationId = Guid.NewGuid().ToString();
            var streams = new List<Stream>() { Stream.Null, Stream.Null };
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                null,
                "Workflow",
                correlationId,
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.QueueAsync(It.Is<string>(p => p == correlationId),
                    It.IsAny<DicomFileStorageMetadata>(),
                    It.IsAny<DataOrigin>(),
                    It.IsAny<uint>(), null),
                Times.Never());
            _inputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Once());
            _inputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()), Times.Never());
        }

        [Fact]
        public async Task GivingValidDicomInstances_WhenSaveingInstanceWithoutVirtualAE_ExpectInstanceToBeQueued()
        {
            var uids = new List<StudySeriesSopAids>();
            SetupDicomToolkitMocks(uids);
            _payloadAssembler.Setup(p => p.QueueAsync(It.IsAny<string>(),
                It.IsAny<DicomFileStorageMetadata>(),
                It.IsAny<DataOrigin>(),
                It.IsAny<uint>(), null));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var correlationId = Guid.NewGuid().ToString();
            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                null,
                "Workflow",
                correlationId,
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

            var referencedSopSequence = result.Data.GetSequence(DicomTag.ReferencedSOPSequence);
            Assert.Equal(streams.Count, referencedSopSequence.Items.Count);

            foreach (var item in referencedSopSequence.Items)
            {
                var uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.Contains(uids, p => p.SopInstanceUid == uid);
                uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                Assert.Contains(uids, p => p.SopClassUid == uid);
            }

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(streams.Count));
            _payloadAssembler.Verify(p => p.QueueAsync(It.Is<string>(p => p == correlationId),
                    It.IsAny<DicomFileStorageMetadata>(),
                    It.IsAny<DataOrigin>(),
                    It.IsAny<uint>(), null),
                Times.Exactly(streams.Count));
            _inputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Once());
            _inputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()), Times.Exactly(streams.Count));
        }

        [Fact]
        public async Task GivingValidDicomInstances_WhenUnableToOpenInstance_ExpectZeroFailedSOPSequence()
        {
            var uids = new List<StudySeriesSopAids>();

            _dicomToolkit.Setup(p => p.OpenAsync(It.IsAny<Stream>(), It.IsAny<FileReadOption>())).Throws(new Exception("error"));
            _payloadAssembler.Setup(p => p.QueueAsync(It.IsAny<string>(),
                It.IsAny<DicomFileStorageMetadata>(),
                It.IsAny<DataOrigin>(),
                It.IsAny<uint>(), null));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var correlationId = Guid.NewGuid().ToString();
            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                null,
                "Workflow",
                correlationId,
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);

            var failedSopSequence = result.Data.GetSequence(DicomTag.FailedSOPSequence);
            Assert.Equal(0, failedSopSequence.Items.Count);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.QueueAsync(It.Is<string>(p => p == correlationId),
                    It.IsAny<DicomFileStorageMetadata>(),
                    It.IsAny<DataOrigin>(),
                    It.IsAny<uint>(), null),
                Times.Never());
            _inputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Once());
            _inputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()), Times.Never());
        }

        [Fact]
        public async Task GivingValidDicomInstances_WhenSavingInstanceWithVirtualAE_ExpectInstanceToBeQueued()
        {
            var vae = new VirtualApplicationEntity
            {
                Name = Guid.NewGuid().ToString(),
                VirtualAeTitle = Guid.NewGuid().ToString(),
                PlugInAssemblies = new List<string> { "A", "B" },
            };
            var uids = new List<StudySeriesSopAids>();
            SetupDicomToolkitMocks(uids);
            _payloadAssembler.Setup(p => p.QueueAsync(It.IsAny<string>(),
                It.IsAny<DicomFileStorageMetadata>(),
                It.IsAny<DataOrigin>(),
                It.IsAny<uint>(), null));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_serviceScopeFactory.Object, _uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object, _fileSystem);

            var correlationId = Guid.NewGuid().ToString();
            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                vae,
                "Workflow",
                correlationId,
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

            var referencedSopSequence = result.Data.GetSequence(DicomTag.ReferencedSOPSequence);
            Assert.Equal(streams.Count, referencedSopSequence.Items.Count);

            foreach (var item in referencedSopSequence.Items)
            {
                var uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.Contains(uids, p => p.SopInstanceUid == uid);
                uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                Assert.Contains(uids, p => p.SopClassUid == uid);
            }

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(streams.Count));
            _payloadAssembler.Verify(p => p.QueueAsync(It.Is<string>(p => p == correlationId),
                    It.IsAny<DicomFileStorageMetadata>(),
                    It.IsAny<DataOrigin>(),
                    It.IsAny<uint>(), null),
                Times.Exactly(streams.Count));
            _inputDataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Once());
            _inputDataPlugInEngine.Verify(p => p.ExecutePlugInsAsync(It.IsAny<DicomFile>(), It.IsAny<FileStorageMetadata>()), Times.Exactly(streams.Count));
        }

        private void SetupDicomToolkitMocks(List<StudySeriesSopAids> uids)
        {
            _dicomToolkit.Setup(p => p.OpenAsync(It.IsAny<Stream>(), It.IsAny<FileReadOption>()))
                            .Returns((Stream stream, FileReadOption fileReadOption) =>
                            {
                                return Task.FromResult(DicomFile.Open(stream, fileReadOption));
                            });

            _dicomToolkit.Setup(p => p.GetStudySeriesSopInstanceUids(It.IsAny<DicomFile>()))
                .Returns((DicomFile dicomFile) =>
                {
                    var uid = new StudySeriesSopAids
                    {
                        SopClassUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID),
                        StudyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID),
                        SeriesInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID),
                        SopInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
                    };
                    uids.Add(uid);
                    return uid;
                });
        }

        private IList<Stream> GenerateDicomStreams(string studyInstanceUid, int count = 3)
        {
            var streams = new List<Stream>();
            for (var i = 0; i < count; i++)
            {
                var instance = InstanceGenerator.GenerateDicomData(studyInstanceUid);
                streams.Add(new MemoryStream(instance));
            }
            return streams;
        }
    }
}
