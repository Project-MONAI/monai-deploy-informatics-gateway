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
using System.Text;
using System.Threading.Tasks;
using FellowOakDicom.Serialization;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class DicomFileStorageMetadataExtensionsTest
    {
        [Fact]
        public async Task GivenADicomFileStorageMetadata_WhenSetDataStreamsIsCalled_ExpectDataStreamsAreSet()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var dicom = InstanceGenerator.GenerateDicomFile();
            var json = dicom.ToJson(DicomJsonOptions.Complete);
            await metadata.SetDataStreams(dicom, json).ConfigureAwait(false);

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