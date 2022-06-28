// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.DicomWeb
{
    public class StreamsWriterTest
    {
        private readonly Mock<IStorageInfoProvider> _storageInfo;
        private readonly Mock<ILogger<StreamsWriter>> _logger;
        private readonly Mock<ITemporaryFileStore> _fileStore;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;

        public StreamsWriterTest()
        {
            _fileStore = new Mock<ITemporaryFileStore>();
            _dicomToolkit = new Mock<IDicomToolkit>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _storageInfo = new Mock<IStorageInfoProvider>();
            _logger = new Mock<ILogger<StreamsWriter>>();
        }

        [Fact(DisplayName = "Constructor Test")]
        public void ConstructorTest()
        {
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_fileStore.Object, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, null));
            var exception = Record.Exception(() => new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object));

            Assert.Null(exception);
        }

        [Fact(DisplayName = "Save - handles error when unable to open stream")]
        public async Task Save_HandlesOpenStreamException()
        {
            _dicomToolkit.Setup(p => p.OpenAsync(It.IsAny<Stream>(), It.IsAny<FileReadOption>()))
                .Throws(new Exception("error"));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object);

            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                "Workflow",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Fact(DisplayName = "Save - adds instance to failure SQ when out of space")]
        public async Task Save_AddsInstanceToFailureSqOnLowSpace()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);
            _storageInfo.SetupGet(p => p.HasSpaceAvailableToStore).Returns(false);

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object);

            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                "Workflow",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);

            var failedSopSequence = result.Data.GetSequence(DicomTag.FailedSOPSequence);
            Assert.Equal(streams.Count, failedSopSequence.Items.Count);

            foreach (var item in failedSopSequence.Items)
            {
                var uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.Contains(uids, p => p.SopInstanceUid == uid);
                uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                Assert.Contains(uids, p => p.SopClassUid == uid);
                var failureReason = item.GetSingleValue<ushort>(DicomTag.FailureReason);
                Assert.Equal(DicomStatus.StorageStorageOutOfResources.Code, failureReason);
            }
        }

        [Fact(DisplayName = "Save - adds instance to failure SQ on IO exception saving instance")]
        public async Task Save_AddsInstanceToFailureSqOnIOExceptionSavingInstance()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);
            _storageInfo.SetupGet(p => p.HasSpaceAvailableToStore).Returns(true);

            _fileStore.Setup(p => p.SaveDicomInstance(It.IsAny<string>(), It.IsAny<DicomFile>(), It.IsAny<CancellationToken>()))
                .Throws(new IOException("error", Constants.ERROR_HANDLE_DISK_FULL));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object);

            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                "Workflow",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);

            var failedSopSequence = result.Data.GetSequence(DicomTag.FailedSOPSequence);
            Assert.Equal(streams.Count, failedSopSequence.Items.Count);

            foreach (var item in failedSopSequence.Items)
            {
                var uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.Contains(uids, p => p.SopInstanceUid == uid);
                uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                Assert.Contains(uids, p => p.SopClassUid == uid);
                var failureReason = item.GetSingleValue<ushort>(DicomTag.FailureReason);
                Assert.Equal(DicomStatus.StorageStorageOutOfResources.Code, failureReason);
            }
        }

        [Fact(DisplayName = "Save - adds instance to failure SQ on exception saving instance")]
        public async Task Save_AddsInstanceToFailureSqOnExceptionSavingInstance()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);
            _storageInfo.SetupGet(p => p.HasSpaceAvailableToStore).Returns(true);

            _fileStore.Setup(p => p.SaveDicomInstance(It.IsAny<string>(), It.IsAny<DicomFile>(), It.IsAny<CancellationToken>()))
                .Throws(new IOException("error"));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object);

            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                "Workflow",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);

            var failedSopSequence = result.Data.GetSequence(DicomTag.FailedSOPSequence);
            Assert.Equal(streams.Count, failedSopSequence.Items.Count);

            foreach (var item in failedSopSequence.Items)
            {
                var uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUID);
                Assert.Contains(uids, p => p.SopInstanceUid == uid);
                uid = item.GetSingleValue<string>(DicomTag.ReferencedSOPClassUID);
                Assert.Contains(uids, p => p.SopClassUid == uid);
                var failureReason = item.GetSingleValue<ushort>(DicomTag.FailureReason);
                Assert.Equal(DicomStatus.ProcessingFailure.Code, failureReason);
            }
        }

        [Fact(DisplayName = "Save - ignores instances with warning when StudyInstanceUIDs don't match")]
        public async Task Save_IgnoresInstancesWithWarning()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);
            _storageInfo.SetupGet(p => p.HasSpaceAvailableToStore).Returns(true);

            _fileStore.Setup(p => p.SaveDicomInstance(It.IsAny<string>(), It.IsAny<DicomFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DicomStoragePaths { FilePath = "path", DicomMetadataFilePath = "path" });
            _payloadAssembler.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<DicomFileStorageInfo>(), It.IsAny<uint>()));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object);

            var correlationId = Guid.NewGuid().ToString();
            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                DicomUIDGenerator.GenerateDerivedFromUUID().UID,
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

            _payloadAssembler.Verify(p => p.Queue(It.Is<string>(p => p == correlationId), It.IsAny<DicomFileStorageInfo>(), It.IsAny<uint>()), Times.Never());
        }

        [Fact(DisplayName = "Save - queues instances with Payload Assembler")]
        public async Task Save_QueuesInstancesWithPayloadAssembler()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);
            _storageInfo.SetupGet(p => p.HasSpaceAvailableToStore).Returns(true);

            _fileStore.Setup(p => p.SaveDicomInstance(It.IsAny<string>(), It.IsAny<DicomFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DicomStoragePaths { FilePath = "path", DicomMetadataFilePath = "path" });
            _payloadAssembler.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<DicomFileStorageInfo>(), It.IsAny<uint>()));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_fileStore.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _storageInfo.Object, _logger.Object);

            var correlationId = Guid.NewGuid().ToString();
            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
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

            _payloadAssembler.Verify(p => p.Queue(It.Is<string>(p => p == correlationId), It.IsAny<DicomFileStorageInfo>(), It.IsAny<uint>()));
        }

        private void SetupDicomToolkitMocks(List<StudySerieSopUids> uids)
        {
            _dicomToolkit.Setup(p => p.OpenAsync(It.IsAny<Stream>(), It.IsAny<FileReadOption>()))
                            .Returns((Stream stream, FileReadOption fileReadOption) =>
                            {
                                return Task.FromResult(DicomFile.Open(stream, fileReadOption));
                            });
            _dicomToolkit.Setup(p => p.GetStudySeriesSopInstanceUids(It.IsAny<DicomFile>()))
                .Returns((DicomFile dicomFile) =>
                {
                    var uid = new StudySerieSopUids
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
