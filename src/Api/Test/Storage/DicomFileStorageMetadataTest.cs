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
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.Messaging.Events;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class DicomFileStorageMetadataTest
    {
        [Fact]
        public void GivenDicomFileStorageMetadata_ExpectToHaveAETitlesSet()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var callingAeTitle = "calling";
            var calledAeTitle = "called";
            var metadata = new DicomFileStorageMetadata(correlationId, identifier, "study", "series", "sop", DataService.DIMSE, callingAeTitle, calledAeTitle);

            Assert.Equal(identifier, metadata.Id);
            Assert.Equal(correlationId, metadata.CorrelationId);
            Assert.Equal(callingAeTitle, metadata.DataOrigin.Source);
            Assert.Equal(calledAeTitle, metadata.DataOrigin.Destination);
            Assert.Equal(DataService.DIMSE, metadata.DataOrigin.DataService);
            Assert.NotNull(metadata.Workflows);
        }

        [Fact]
        public void GivenDicomFileStorageMetadata_ExpectToHaveCorrectFilePathsSet()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var callingAeTitle = "calling";
            var calledAeTitle = "called";

            var metadata = new DicomFileStorageMetadata(correlationId, identifier, "study", "series", "sop", DataService.DIMSE, callingAeTitle, calledAeTitle);

            Assert.Equal($"{DicomFileStorageMetadata.DicomSubDirectoryName}/study/series/sop{DicomFileStorageMetadata.FileExtension}", metadata.File.UploadPath);
            Assert.StartsWith($"{correlationId}/{DicomFileStorageMetadata.DicomSubDirectoryName}/", metadata.File.TemporaryPath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(DicomFileStorageMetadata.FileExtension, metadata.File.TemporaryPath, StringComparison.OrdinalIgnoreCase);

            Assert.Equal($"{DicomFileStorageMetadata.DicomSubDirectoryName}/study/series/sop{DicomFileStorageMetadata.FileExtension}{DicomFileStorageMetadata.DicomJsonFileExtension}", metadata.JsonFile.UploadPath);
            Assert.StartsWith($"{correlationId}/{DicomFileStorageMetadata.DicomSubDirectoryName}/", metadata.JsonFile.TemporaryPath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(DicomFileStorageMetadata.DicomJsonFileExtension, metadata.JsonFile.TemporaryPath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GivenDicomFileStorageMetadata_WhenSetFailedIsCalled_AllFilesAreSetToFailed()
        {
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var callingAeTitle = "calling";
            var calledAeTitle = "called";

            var metadata = new DicomFileStorageMetadata(correlationId, identifier, "study", "series", "sop", DataService.DIMSE, callingAeTitle, calledAeTitle);

            metadata.SetFailed();

            Assert.True(metadata.File.IsUploadFailed);
            Assert.True(metadata.JsonFile.IsUploadFailed);
        }

        [Fact]
        public void GivenDicomFileStorageMetadata_WhenGetPayloadPathIsCalled_APayyloadPathIsReturned()
        {
            var payloadId = Guid.NewGuid();
            var correlationId = Guid.NewGuid().ToString();
            var identifier = Guid.NewGuid().ToString();
            var callingAeTitle = "calling";
            var calledAeTitle = "called";

            var metadata = new DicomFileStorageMetadata(correlationId, identifier, "study", "series", "sop", DataService.DIMSE, callingAeTitle, calledAeTitle);

            Assert.Equal($"{payloadId}/{metadata.File.UploadPath}", metadata.File.GetPayloadPath(payloadId));
            Assert.Equal($"{payloadId}/{metadata.JsonFile.UploadPath}", metadata.JsonFile.GetPayloadPath(payloadId));
        }


        [Fact]
        public void StudyInstanceUid_Set_ValidValue()
        {
            // Arrange
            var metadata = new DicomFileStorageMetadata();

            // Act
            metadata.StudyInstanceUid = "12345";

            // Assert
            Assert.Equal("12345", metadata.StudyInstanceUid);
        }

        [Fact]
        public void SeriesInstanceUid_Set_ValidValue()
        {
            // Arrange
            var metadata = new DicomFileStorageMetadata { SeriesInstanceUid = "67890" };

            // Assert
            Assert.Equal("67890", metadata.SeriesInstanceUid);
        }

        [Fact]
        public void SopInstanceUid_Set_ValidValue()
        {
            // Arrange
            var metadata = new DicomFileStorageMetadata { SopInstanceUid = "ABCDE" };

            // Assert
            Assert.Equal("ABCDE", metadata.SopInstanceUid);
        }

    }
}
