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
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.SharedTest;
using MongoDB.Bson;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class DicomFileStorageMetadataExtensionsTest
    {
        [Fact]
        public async Task GivenADicomFileStorageMetadata_WhenSetDataStreamsIsCalledWithInMemoryStore_ExpectDataStreamsAreSet()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var dicom = InstanceGenerator.GenerateDicomFile();
            var json = dicom.ToJson(DicomJsonOptions.Complete, false);
            await metadata.SetDataStreams(dicom, json, TemporaryDataStorageLocation.Memory).ConfigureAwait(false);

            Assert.NotNull(metadata.File.Data);
            Assert.NotNull(metadata.JsonFile.Data);

            var ms = new MemoryStream();
            await dicom.SaveAsync(ms).ConfigureAwait(false);
            Assert.Equal(ms.ToArray(), (metadata.File.Data as MemoryStream).ToArray());

            var jsonFromStream = Encoding.UTF8.GetString((metadata.JsonFile.Data as MemoryStream).ToArray());
            Assert.Equal(json.Trim(), jsonFromStream.Trim());

            var dicomFileFromJson = DicomJson.ConvertJsonToDicom(json);
            Assert.Equal(dicom.Dataset, dicomFileFromJson);
        }

        [Fact]
        public async Task GivenADicomFileStorageMetadata_WhenSetDataStreamsIsCalledWithDiskStore_ExpectDataStreamsAreSet()
        {
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("/temp");

            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var dicom = InstanceGenerator.GenerateDicomFile();
            var json = dicom.ToJson(DicomJsonOptions.Complete, false);
            await metadata.SetDataStreams(dicom, json, TemporaryDataStorageLocation.Disk, fileSystem, "/temp").ConfigureAwait(false);

            Assert.NotNull(metadata.File.Data);
            Assert.NotNull(metadata.JsonFile.Data);

            var ms = new MemoryStream();
            await dicom.SaveAsync(ms).ConfigureAwait(false);
            using var temporaryDataAsMemoryStream = new MemoryStream();
            metadata.File.Data.CopyTo(temporaryDataAsMemoryStream);

            Assert.Equal(ms.ToArray(), temporaryDataAsMemoryStream.ToArray());

            using var temporaryJsonDataAsMemoryStream = new MemoryStream();
            metadata.JsonFile.Data.CopyTo(temporaryJsonDataAsMemoryStream);

            var jsonFromStream = Encoding.UTF8.GetString(temporaryJsonDataAsMemoryStream.ToArray());
            Assert.Equal(json.Trim(), jsonFromStream.Trim());

            var dicomFileFromJson = DicomJson.ConvertJsonToDicom(json);
            Assert.Equal(dicom.Dataset, dicomFileFromJson);
        }

        [Fact]
        public void GivenADicomFileStorageMetadataWithInvalidDSValue_WhenSetDataStreamsIsCalledWithValidation_ThrowsFormatException()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var dicom = InstanceGenerator.GenerateDicomFile();
#pragma warning disable CS0618 // Type or member is obsolete
            dicom.Dataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete
            dicom.Dataset.Add(DicomTag.PixelSpacing, "0.68300002813334234234392", "0.235425246583524352345");

            Assert.Throws<FormatException>(() => dicom.ToJson(DicomJsonOptions.Complete, true));
        }

        [Fact]
        public async Task GivenADicomFileStorageMetadataWithInvalidDSValue_WhenSetDataStreamsIsCalled_ExpectDataStreamsAreSet()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var dicom = InstanceGenerator.GenerateDicomFile();
#pragma warning disable CS0618 // Type or member is obsolete
            dicom.Dataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete
            dicom.Dataset.Add(DicomTag.PixelSpacing, "0.68300002813334234392234", "0.2354257587243524352345");

            var json = dicom.ToJson(DicomJsonOptions.Complete, false);
            await metadata.SetDataStreams(dicom, json, TemporaryDataStorageLocation.Memory).ConfigureAwait(false);

            Assert.NotNull(metadata.File.Data);
            Assert.NotNull(metadata.JsonFile.Data);

            var ms = new MemoryStream();
            await dicom.SaveAsync(ms).ConfigureAwait(false);
            Assert.Equal(ms.ToArray(), (metadata.File.Data as MemoryStream).ToArray());

            var jsonFromStream = Encoding.UTF8.GetString((metadata.JsonFile.Data as MemoryStream).ToArray());
            Assert.Equal(json.Trim(), jsonFromStream.Trim());

            var dicomFileFromJson = DicomJson.ConvertJsonToDicom(json);
            Assert.Equal(dicom.Dataset, dicomFileFromJson);
        }
    }
}
