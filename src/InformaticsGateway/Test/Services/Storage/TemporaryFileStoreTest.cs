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
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Storage
{
    public class TemporaryFileStoreTest
    {
        private const long OneGB = 1000000000;
        private readonly string _transactionId;
        private readonly Mock<ILogger<TemporaryFileStore>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IDicomToolkit> _dicomToolkit;
        private MockFileSystem _fileSystem;

        public TemporaryFileStoreTest()
        {
            _transactionId = Guid.NewGuid().ToString();
            _logger = new Mock<ILogger<TemporaryFileStore>>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _dicomToolkit = new Mock<IDicomToolkit>();
            _dicomToolkit.Setup(p => p.HasValidHeader(It.IsAny<string>())).Returns((string filename) =>
            {
                return Path.GetExtension(filename).Equals(".dcm", StringComparison.OrdinalIgnoreCase);
            });

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(DisplayName = "TemporaryFileStore shall restore files")]
        public void ShallRestoreFiles()
        {
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, DicomFileStorageInfo.DicomSubDirectoryName, "dicom1.dcm"), new MockFileData(InstanceGenerator.GenerateDicomData()) },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, DicomFileStorageInfo.DicomSubDirectoryName, "dicom1.dcm.json"), new MockFileData("[]") },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, DicomFileStorageInfo.DicomSubDirectoryName, "dicom2.dcm"), new MockFileData(InstanceGenerator.GenerateDicomData()) },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, FhirFileStorageInfo.FhirSubDirectoryName, "patient-1.xml"), new MockFileData("<xml />") },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, FhirFileStorageInfo.FhirSubDirectoryName, "observation-2.json"), new MockFileData("{}") },
            });

            var fileStore = new TemporaryFileStore(_fileSystem, _logger.Object, _configuration, _dicomToolkit.Object);
            var result = fileStore.RestoreInferenceRequestFiles(_transactionId, CancellationToken.None);

            Assert.Equal(4, result.Count);

            var count = 0;
            foreach (var file in result)
            {
                if (file is DicomStoragePaths dicom)
                {
                    if (Path.GetFileName(dicom.FilePath).Equals("dicom1.dcm", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Equal("dicom1.dcm.json", Path.GetFileName(dicom.DicomMetadataFilePath));
                        count++;
                    }
                    if (Path.GetFileName(dicom.FilePath).Equals("dicom2.dcm", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Null(dicom.DicomMetadataFilePath);
                        count++;
                    }
                }
                else if (file is FhirStoragePath fhir)
                {
                    if (Path.GetFileName(fhir.FilePath).Equals("patient-1.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Equal("patient", fhir.ResourceType);
                        Assert.Equal("1", fhir.ResourceId);
                        count++;
                    }
                    if (Path.GetFileName(fhir.FilePath).Equals("observation-2.json", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.Equal("observation", fhir.ResourceType);
                        Assert.Equal("2", fhir.ResourceId);
                        count++;
                    }
                }
            }
            Assert.Equal(4, count);
        }

        [RetryFact(DisplayName = "TemporaryFileStore shall save DICOM instance")]
        public async Task ShallSaveDicomInstance()
        {
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var seriesInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var sopInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var dicomFile = InstanceGenerator.GenerateDicomFile(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            var uids = new StudySerieSopUids
            {
                StudyInstanceUid = studyInstanceUid,
                SeriesInstanceUid = seriesInstanceUid,
                SopInstanceUid = sopInstanceUid,
            };
            _dicomToolkit.Setup(p => p.GetStudySeriesSopInstanceUids(It.IsAny<DicomFile>()))
                .Returns(uids);

            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, DicomFileStorageInfo.DicomSubDirectoryName, "dicom1.dcm"), new MockFileData(InstanceGenerator.GenerateDicomData()) },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, DicomFileStorageInfo.DicomSubDirectoryName, "dicom1.dcm.json"), new MockFileData("[]") },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, DicomFileStorageInfo.DicomSubDirectoryName, "dicom2.dcm"), new MockFileData(InstanceGenerator.GenerateDicomData()) },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, FhirFileStorageInfo.FhirSubDirectoryName, "patient-1.xml"), new MockFileData("<xml />") },
                { Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, _transactionId, FhirFileStorageInfo.FhirSubDirectoryName, "observation-2.json"), new MockFileData("{}") },
            });

            var fileStore = new TemporaryFileStore(_fileSystem, _logger.Object, _configuration, _dicomToolkit.Object);

            var result = await fileStore.SaveDicomInstance(_transactionId, dicomFile, CancellationToken.None);

            _dicomToolkit.Verify(p => p.Save(It.IsAny<DicomFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DicomJsonOptions>()), Times.Once());

            Assert.Equal(studyInstanceUid, result.UIDs.StudyInstanceUid);
            Assert.Equal(seriesInstanceUid, result.UIDs.SeriesInstanceUid);
            Assert.Equal(sopInstanceUid, result.UIDs.SopInstanceUid);
            Assert.Equal(uids.Identifier, result.Identifier);
        }

        [RetryFact(DisplayName = "TemporaryFileStore shall save FHIR resource")]
        public async Task ShallSaveFhirResource()
        {
            _fileSystem = new MockFileSystem();
            var fileStore = new TemporaryFileStore(_fileSystem, _logger.Object, _configuration, _dicomToolkit.Object);
            var type = "Observation";
            var id = Guid.NewGuid().ToString();
            var data = "data";

            var result = await fileStore.SaveFhirResource(_transactionId, type, id, Api.Rest.FhirStorageFormat.Json, data, CancellationToken.None);

            Assert.NotEmpty(_fileSystem.AllFiles);

            var savedData = _fileSystem.File.ReadAllText(_fileSystem.AllFiles.Last());
            Assert.Equal(data, savedData);

            Assert.Equal(type, result.ResourceType);
            Assert.Equal(id, result.ResourceId);
        }
    }
}
