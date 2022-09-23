/*
 * Copyright 2022 MONAI Consortium
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
        private readonly Mock<ILogger<StreamsWriter>> _logger;
        private readonly Mock<IObjectUploadQueue> _uploadQueue;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;

        public StreamsWriterTest()
        {
            _uploadQueue = new Mock<IObjectUploadQueue>();
            _dicomToolkit = new Mock<IDicomToolkit>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _logger = new Mock<ILogger<StreamsWriter>>();
        }

        [Fact]
        public void GivenAStreamsWriter_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_uploadQueue.Object, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, null));
            var exception = Record.Exception(() => new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object));

            Assert.Null(exception);
        }

        [Fact]
        public async Task GivenAHttpStream_WhenFailedToOpenAsDicomInstance_ExpectStatus409ToBeReturned()
        {
            _dicomToolkit.Setup(p => p.OpenAsync(It.IsAny<Stream>(), It.IsAny<FileReadOption>()))
                .Throws(new Exception("error"));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object);

            var streams = GenerateDicomStreams(studyInstanceUid);
            var result = await writer.Save(
                streams,
                studyInstanceUid,
                "Workflow",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Fact]
        public async Task GivingADicomInstanceWithoutAMatchingStudyInstanceUid_WhenSaveingInstance_ExpectAWarningAndNotStored()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);

            _payloadAssembler.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<DicomFileStorageMetadata>(), It.IsAny<uint>()));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object);

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

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.Is<string>(p => p == correlationId), It.IsAny<DicomFileStorageMetadata>(), It.IsAny<uint>()), Times.Never());
        }

        [Fact]
        public async Task GivingAValidDicomInstance_WhenSaveingInstance_ExpectInstanceToBeQueued()
        {
            var uids = new List<StudySerieSopUids>();
            SetupDicomToolkitMocks(uids);
            _payloadAssembler.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<DicomFileStorageMetadata>(), It.IsAny<uint>()));

            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var writer = new StreamsWriter(_uploadQueue.Object, _dicomToolkit.Object, _payloadAssembler.Object, _configuration, _logger.Object);

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

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(streams.Count));
            _payloadAssembler.Verify(p => p.Queue(It.Is<string>(p => p == correlationId), It.IsAny<DicomFileStorageMetadata>(), It.IsAny<uint>()), Times.Exactly(streams.Count));
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
