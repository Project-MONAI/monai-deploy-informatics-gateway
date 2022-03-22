// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
