// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Dicom;
using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Common.Test
{
    public class DicomToolkitTest
    {
        private IFileSystem _fileSystem;

        public DicomToolkitTest()
        {
            _fileSystem = new MockFileSystem();
        }

        [Fact(DisplayName = "HasValidHeder - false when reading a text file")]
        public void HasValidHeader_False()
        {
            var filename = Path.GetTempFileName();
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("Hello World!");
            }

            var dicomToolkit = new DicomToolkit(_fileSystem);
            Assert.False(dicomToolkit.HasValidHeader(filename));
        }

        [Fact(DisplayName = "HasValidHeder - true with a valid DICOM file")]
        public void HasValidHeader_True()
        {
            var filename = Path.GetTempFileName();
            var dicomFile = new DicomFile();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            dicomFile.Save(filename);

            var dicomToolkit = new DicomToolkit(_fileSystem);
            Assert.True(dicomToolkit.HasValidHeader(filename));
        }

        [Fact(DisplayName = "Open - a valid DICOM file")]
        public void Open_ValidFile()
        {
            var filename = Path.GetTempFileName();
            var dicomFile = new DicomFile();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var directory = _fileSystem.Path.GetDirectoryName(filename);
            _fileSystem.Directory.CreateDirectoryIfNotExists(directory);
            using var stream = _fileSystem.File.Create(filename);
            dicomFile.Save(stream);

            var dicomToolkit = new DicomToolkit(_fileSystem);
            dicomToolkit.Open(filename);
        }

        [Fact(DisplayName = "TryGetString - missing DICOM tag")]
        public void TryGetString_MissingDicomTag()
        {
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var dicomToolkit = new DicomToolkit(_fileSystem);
            Assert.False(dicomToolkit.TryGetString(dicomFile, DicomTag.StudyInstanceUID, out var sopInstanceUId));
            Assert.Equal(string.Empty, sopInstanceUId);
        }

        [Fact(DisplayName = "TryGetString - a valid DICOM file")]
        public void TryGetString_ValidFile()
        {
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var dicomToolkit = new DicomToolkit(_fileSystem);
            Assert.True(dicomToolkit.TryGetString(dicomFile, DicomTag.SOPInstanceUID, out var sopInstanceUId));
            Assert.Equal(expectedSop.UID, sopInstanceUId);
        }

        [Fact(DisplayName = "Save - a valid DICOM file")]
        public async Task Save_ValidFile()
        {
            var filename = Path.GetTempFileName();
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var dicomToolkit = new DicomToolkit(_fileSystem);
            await dicomToolkit.Save(dicomFile, filename);

            var savedFile = dicomToolkit.Open(filename);

            Assert.NotNull(savedFile);

            Assert.Equal(expectedSop.UID, savedFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
        }

        [Fact(DisplayName = "Load - throws when data is invalid")]
        public void Load_ThrowsWithBadData()
        {
            var random = new Random();
            var bytes = new byte[10];
            random.NextBytes(bytes);

            var dicomToolkit = new DicomToolkit(_fileSystem);
            Assert.Throws<DicomDataException>(() => (dicomToolkit.Load(bytes)));
        }

        [Fact(DisplayName = "Load - returns DicomFile")]
        public void Load_ReturnsDicomFile()
        {
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

            using var memoryStream = new MemoryStream();
            dicomFile.Save(memoryStream);

            var dicomToolkit = new DicomToolkit(_fileSystem);
            var result = dicomToolkit.Load(memoryStream.ToArray());

            Assert.Equal(dicomFile.FileMetaInfo.TransferSyntax, result.FileMetaInfo.TransferSyntax);
            Assert.Equal(expectedSop, result.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID));
        }
    }
}