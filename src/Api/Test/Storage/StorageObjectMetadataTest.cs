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
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class StorageObjectMetadataTest
    {
        [Fact]
        public void GivenAStorageObjectMetadata_InitializeWithFileExtensionMissingDot_ExpectADotToBePrepended()
        {
            var metadata = new StorageObjectMetadata("txt");

            Assert.Equal(".txt", metadata.FileExtension);
        }

        [Fact]
        public void GivenAStorageObjectMetadata_WhenSetUploadIsCalled_ExpectUplaodValuesToBeSetAndDataStreamDisposed()
        {
            var metadata = new StorageObjectMetadata(".txt");
            metadata.Data = new MemoryStream();
            metadata.SetUploaded("MYBUCKET");

            Assert.Equal("MYBUCKET", metadata.TemporaryBucketName);
            Assert.NotNull(metadata.DateUploaded);
            Assert.Equal(DateTime.UtcNow, metadata.DateUploaded.Value, TimeSpan.FromSeconds(1));
            Assert.True(metadata.IsUploaded);

            // Verify that the data stream is closed
            Assert.Throws<NullReferenceException>(() => metadata.Data.ReadByte());

            // Calling it twice should not throw any exception
            var exception = Record.Exception(() => metadata.SetUploaded("MYBUCKET"));
            Assert.Null(exception);
        }
    }
}
