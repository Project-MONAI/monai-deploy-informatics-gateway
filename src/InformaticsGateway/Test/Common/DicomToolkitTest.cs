// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Serialization;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class DicomToolkitTest
    {
        private readonly IFileSystem _fileSystem;

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

            var fileSystem = new FileSystem();
            var directory = fileSystem.Path.GetDirectoryName(filename);
            fileSystem.Directory.CreateDirectoryIfNotExists(directory);
            using var stream = fileSystem.File.Create(filename);
            dicomFile.Save(stream);
            stream.Close();

            var dicomToolkit = new DicomToolkit(fileSystem);
            var openedDicomFile = dicomToolkit.Open(filename);

            Assert.NotNull(openedDicomFile);
            Assert.Equal(dicomFile.FileMetaInfo.TransferSyntax, openedDicomFile.FileMetaInfo.TransferSyntax);
            Assert.Equal(dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID, openedDicomFile.FileMetaInfo.MediaStorageSOPInstanceUID);
            Assert.Equal(dicomFile.FileMetaInfo.MediaStorageSOPClassUID, openedDicomFile.FileMetaInfo.MediaStorageSOPClassUID);
            Assert.Equal(dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID), openedDicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));
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

        [Fact(DisplayName = "Save - a valid DICOM file without json")]
        public async Task Save_ValidFileWithoutJson()
        {
            var filename = Path.GetTempFileName();
            var jsonFilename = $"{filename}.json";
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var dicomToolkit = new DicomToolkit(_fileSystem);
            await dicomToolkit.Save(dicomFile, filename, jsonFilename, Configuration.DicomJsonOptions.None);

            var savedFile = dicomToolkit.Open(filename);

            Assert.NotNull(savedFile);

            Assert.Equal(expectedSop.UID, savedFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.False(_fileSystem.File.Exists(jsonFilename));
        }

        [Fact(DisplayName = "Save - a valid DICOM file without other value types")]
        public async Task Save_ValidFileWithoutOtherValueTypes()
        {
            var filename = Path.GetTempFileName();
            var jsonFilename = $"{filename}.json";
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var dicomToolkit = new DicomToolkit(_fileSystem);
            await dicomToolkit.Save(dicomFile, filename, jsonFilename, Configuration.DicomJsonOptions.IgnoreOthers);

            var savedFile = dicomToolkit.Open(filename);

            Assert.NotNull(savedFile);

            Assert.Equal(expectedSop.UID, savedFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.True(_fileSystem.File.Exists(jsonFilename));

            Assert.False(savedFile.Dataset.TryGetValues<byte[]>(DicomTag.PixelData, out _));
        }

        [Fact(DisplayName = "Save - a valid DICOM file with complete JSON")]
        public async Task Save_ValidFileWithJson()
        {
            ushort rows = 10, columns = 10;
            var filename = Path.GetTempFileName();
            var jsonFilename = $"{filename}.json";
            var dicomFile = new DicomFile();
            var expectedSop = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.Dataset.Add(DicomTag.SOPInstanceUID, expectedSop)
                             .AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Monochrome2.Value)
                             .AddOrUpdate<ushort>(DicomTag.Rows, rows)
                             .AddOrUpdate<ushort>(DicomTag.Columns, columns)
                             .AddOrUpdate<ushort>(DicomTag.BitsAllocated, 8)
                             .AddOrUpdate<ushort>(DicomTag.BitsStored, 8)
                             .AddOrUpdate<ushort>(DicomTag.HighBit, 7)
                             .AddOrUpdate(DicomTag.PixelRepresentation, (ushort)PixelRepresentation.Unsigned)
                             .AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)PlanarConfiguration.Interleaved)
                             .AddOrUpdate<ushort>(DicomTag.SamplesPerPixel, 1);
            dicomFile.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            dicomFile.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUIDGenerator.GenerateDerivedFromUUID();
            dicomFile.FileMetaInfo.MediaStorageSOPClassUID = DicomUIDGenerator.GenerateDerivedFromUUID();

            var pixelData = DicomPixelData.Create(dicomFile.Dataset, true);
            var random = new Random();
            var pixels = new byte[rows * columns];
            random.NextBytes(pixels);
            var buffer = new MemoryByteBuffer(pixels);
            pixelData.AddFrame(buffer);

            var dicomToolkit = new DicomToolkit(_fileSystem);
            await dicomToolkit.Save(dicomFile, filename, jsonFilename, Configuration.DicomJsonOptions.Complete);

            var savedFile = dicomToolkit.Open(filename);

            Assert.NotNull(savedFile);

            Assert.Equal(expectedSop.UID, savedFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.True(_fileSystem.File.Exists(jsonFilename));

            var jsonToDicom = DicomJson.ConvertJsonToDicom(_fileSystem.File.ReadAllText(jsonFilename));
            Assert.Equal(expectedSop.UID, jsonToDicom.GetSingleValue<string>(DicomTag.SOPInstanceUID));

            var pixelDataFromJson = DicomPixelData.Create(jsonToDicom);
            var pixelsFromJson = pixelDataFromJson.GetFrame(0).Data;

            Assert.Equal(pixels, pixelsFromJson);
        }

        [Fact(DisplayName = "Load - throws when data is invalid")]
        public void Load_ThrowsWithBadData()
        {
            var random = new Random();
            var bytes = new byte[10];
            random.NextBytes(bytes);

            var dicomToolkit = new DicomToolkit(_fileSystem);
            Assert.Throws<DicomFileException>(() => (dicomToolkit.Load(bytes)));
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
