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

using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public record FhirFileStorageInfo : FileStorageInfo
    {
        const string DirectoryPath = "ehr";

        public string ResourceType { get; set; }

        public FhirFileStorageInfo() { }

        public FhirFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    FhirStorageFormat fhirFileFormat)
            : base(correlationId, storageRootPath, messageId, fhirFileFormat == FhirStorageFormat.Json ? ".json" : ".xml", new FileSystem()) { }

        public FhirFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    FhirStorageFormat fhirFileFormat,
                                    IFileSystem fileSystem)
            : base(correlationId, storageRootPath, messageId, fhirFileFormat == FhirStorageFormat.Json ? ".json" : ".xml", fileSystem)
        {
        }

        protected override string GenerateStoragePath()
        {
            Guard.Against.NullOrWhiteSpace(ResourceType, nameof(ResourceType));
            Guard.Against.NullOrWhiteSpace(MessageId, nameof(MessageId));

            string filePath = System.IO.Path.Combine(StorageRootPath, DirectoryPath, ResourceType, MessageId) + FileExtension;
            filePath = filePath.ToLowerInvariant();
            var index = 1;
            while (FileSystem.File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(StorageRootPath, DirectoryPath, ResourceType, $"{MessageId}-{index++}") + FileExtension;
                filePath = filePath.ToLowerInvariant();
            }

            return filePath;
        }
    }
}
