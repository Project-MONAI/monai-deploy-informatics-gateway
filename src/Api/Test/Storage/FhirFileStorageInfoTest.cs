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
using System.IO;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class FhirFileStorageInfoTest
    {
        [Fact(DisplayName = "Shall return FHIR resource upload path")]
        public void ShallReturUploadFilePaths()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = $"{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}";
            var transactionId = Guid.NewGuid().ToString();
            var filePath = Path.Combine(root, "resource-id.xml");
            var fileStorageInfo = new FhirFileStorageInfo(FhirStorageFormat.Xml)
            {
                CorrelationId = correlationId,
                Source = transactionId,
                ResourceId = "ID",
                ResourceType = "TYPE",
                FilePath = filePath,
            };

            Assert.Equal(
                $"{FhirFileStorageInfo.FhirSubDirectoryName}/{fileStorageInfo.ResourceType}-{fileStorageInfo.ResourceId}{FhirFileStorageInfo.XmlFilExtension}",
                fileStorageInfo.UploadFilePath);

            Assert.Collection(fileStorageInfo.FilePaths,
                (path) => path.Equals(fileStorageInfo.UploadFilePath));

            Assert.Equal("ID", fileStorageInfo.ResourceId);
            Assert.Equal("TYPE", fileStorageInfo.ResourceType);
        }

        [Fact(DisplayName = "Shall return where data is stored for storage service")]
        public void ShallReturnBlockStorageInfo()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = $"{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}";
            var transactionId = Guid.NewGuid().ToString();
            var filePath = Path.Combine(root, "resource-id.json");
            var fileStorageInfo = new FhirFileStorageInfo(FhirStorageFormat.Json)
            {
                CorrelationId = correlationId,
                Source = transactionId,
                ResourceId = "ID",
                ResourceType = "TYPE",
                FilePath = filePath,
            };

            var blockStorage = fileStorageInfo.ToBlockStorageInfo("bucket");
            Assert.Equal(fileStorageInfo.UploadFilePath, blockStorage.Path);
            Assert.Equal(String.Empty, blockStorage.Metadata);
        }
    }
}
