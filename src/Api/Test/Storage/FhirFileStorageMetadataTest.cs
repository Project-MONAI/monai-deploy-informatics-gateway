/*
 * Copyright 2021-2023 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.Messaging.Events;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class FhirFileStorageMetadataTest
    {
        [Theory(DisplayName = "Shall return FHIR resource upload path")]
        [InlineData(FhirStorageFormat.Xml, FhirFileStorageMetadata.XmlFileExtension)]
        [InlineData(FhirStorageFormat.Json, FhirFileStorageMetadata.JsonFilExtension)]
        public void GivenFhirFileStorageMetadataWithSpecifiedFormat_ExpectToHaveCorrectFileExtension(FhirStorageFormat fileFormat, string fileExtension)
        {
            var correlationId = Guid.NewGuid().ToString();
            var metadata = new FhirFileStorageMetadata(correlationId, "TYPE", "ID", fileFormat, DataService.FHIR, correlationId)
            {
                ResourceId = "ID",
                ResourceType = "TYPE",
            };

            Assert.Equal(correlationId, metadata.DataOrigin.Source);
            Assert.Equal(
                $"{FhirFileStorageMetadata.FhirSubDirectoryName}/{metadata.ResourceType}/{metadata.ResourceId}{fileExtension}",
                metadata.File.UploadPath);
            Assert.StartsWith($"{correlationId}/{FhirFileStorageMetadata.FhirSubDirectoryName}/", metadata.File.TemporaryPath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(fileExtension, metadata.File.TemporaryPath, StringComparison.OrdinalIgnoreCase);

            Assert.Equal("ID", metadata.ResourceId);
            Assert.Equal("TYPE", metadata.ResourceType);
        }

        [Fact]
        public void GivenFhirFileStorageMetadata_WhenSetFailedIsCalled_AllFilesAreSetToFailed()
        {
            var correlationId = Guid.NewGuid().ToString();
            var metadata = new FhirFileStorageMetadata(correlationId, "TYPE", "ID", FhirStorageFormat.Xml, DataService.FHIR, "origin")
            {
                ResourceId = "ID",
                ResourceType = "TYPE",
            };

            metadata.SetFailed();

            Assert.True(metadata.File.IsUploadFailed);
        }

        [Fact]
        public void GivenFhirFileStorageMetadata_WhenGetPayloadPathIsCalled_APayyloadPathIsReturned()
        {
            var payloadId = Guid.NewGuid();
            var correlationId = Guid.NewGuid().ToString();
            var metadata = new FhirFileStorageMetadata(correlationId, "TYPE", "ID", FhirStorageFormat.Xml, DataService.FHIR, "origin")
            {
                ResourceId = "ID",
                ResourceType = "TYPE",
            };

            Assert.Equal($"{payloadId}/{metadata.File.UploadPath}", metadata.File.GetPayloadPath(payloadId));
        }
    }
}