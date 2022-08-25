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

using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.Test
{
    public class StorageMetadataWrapperTest
    {
        [Fact]
        public void GivenAFhirFileStorageMetadataObject_WhenInitializedWithStorageMetadataWrapper_ExpectValuesToBeSetCorrectly()
        {
            var metadata = new FhirFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Api.Rest.FhirStorageFormat.Json);
            metadata.SetWorkflows("A", "B", "C");

            var wrapper = new StorageMetadataWrapper(metadata);

            Assert.Equal(metadata.CorrelationId, wrapper.CorrelationId);
            Assert.Equal(metadata.Id, wrapper.Identity);
            Assert.Equal(metadata.GetType().AssemblyQualifiedName, wrapper.TypeName);

            var unwrapped = wrapper.GetObject() as FhirFileStorageMetadata;

            Assert.Equal(metadata.CorrelationId, unwrapped!.CorrelationId);
            Assert.Equal(metadata.Id, unwrapped.Id);
            Assert.Equal(metadata.DataTypeDirectoryName, unwrapped.DataTypeDirectoryName);
            Assert.Equal(metadata.DateReceived, unwrapped.DateReceived);
            Assert.Equal(metadata.IsUploaded, unwrapped.IsUploaded);
            Assert.Equal(metadata.ResourceId, unwrapped.ResourceId);
            Assert.Equal(metadata.ResourceType, unwrapped.ResourceType);
            Assert.Equal(metadata.Source, unwrapped.Source);
            Assert.Equal(metadata.TransactionId, unwrapped.TransactionId);
            Assert.Equal(metadata.Workflows, unwrapped.Workflows);

            Assert.Equal(metadata.File.FileExtension, unwrapped.File.FileExtension);
            Assert.Equal(metadata.File.UploadPath, unwrapped.File.UploadPath);
            Assert.Equal(metadata.File.TemporaryPath, unwrapped.File.TemporaryPath);
            Assert.Equal(metadata.File.IsUploadFailed, unwrapped.File.IsUploadFailed);
            Assert.Equal(metadata.File.ContentType, unwrapped.File.ContentType);
        }

        [Fact]
        public void GivenADicomFileStorageMetadataObject_WhenInitializedWithStorageMetadataWrapper_ExpectValuesToBeSetCorrectly()
        {
            var metadata = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString())
            {
                CalledAeTitle = "CALLEDAET",
                Source = "SOURCE",
            };
            metadata.SetWorkflows("A", "B", "C");

            var wrapper = new StorageMetadataWrapper(metadata);

            Assert.Equal(metadata.CorrelationId, wrapper.CorrelationId);
            Assert.Equal(metadata.Id, wrapper.Identity);
            Assert.Equal(metadata.GetType().AssemblyQualifiedName, wrapper.TypeName);

            var unwrapped = wrapper.GetObject() as DicomFileStorageMetadata;

            Assert.Equal(metadata.CalledAeTitle, unwrapped!.CalledAeTitle);
            Assert.Equal(metadata.CallingAeTitle, unwrapped.CallingAeTitle);
            Assert.Equal(metadata.CorrelationId, unwrapped.CorrelationId);
            Assert.Equal(metadata.Id, unwrapped.Id);
            Assert.Equal(metadata.DataTypeDirectoryName, unwrapped.DataTypeDirectoryName);
            Assert.Equal(metadata.DateReceived, unwrapped.DateReceived);
            Assert.Equal(metadata.IsUploaded, unwrapped.IsUploaded);
            Assert.Equal(metadata.SeriesInstanceUid, unwrapped.SeriesInstanceUid);
            Assert.Equal(metadata.SopInstanceUid, unwrapped.SopInstanceUid);
            Assert.Equal(metadata.StudyInstanceUid, unwrapped.StudyInstanceUid);
            Assert.Equal(metadata.Source, unwrapped.Source);
            Assert.Equal(metadata.Workflows, unwrapped.Workflows);

            Assert.Equal(metadata.File.FileExtension, unwrapped.File.FileExtension);
            Assert.Equal(metadata.File.UploadPath, unwrapped.File.UploadPath);
            Assert.Equal(metadata.File.TemporaryPath, unwrapped.File.TemporaryPath);
            Assert.Equal(metadata.File.IsUploadFailed, unwrapped.File.IsUploadFailed);
            Assert.Equal(metadata.File.ContentType, unwrapped.File.ContentType);

            Assert.Equal(metadata.JsonFile.FileExtension, unwrapped.JsonFile.FileExtension);
            Assert.Equal(metadata.JsonFile.UploadPath, unwrapped.JsonFile.UploadPath);
            Assert.Equal(metadata.JsonFile.TemporaryPath, unwrapped.JsonFile.TemporaryPath);
            Assert.Equal(metadata.JsonFile.IsUploadFailed, unwrapped.JsonFile.IsUploadFailed);
            Assert.Equal(metadata.JsonFile.ContentType, unwrapped.JsonFile.ContentType);
        }
    }
}
