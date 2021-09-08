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
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public record FileStorageInfo
    {
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Gets the correlation ID of the file.
        /// For SCP received DICOM instances: use internally generated unique association ID.
        /// For ACR retrieved DICOM/FHIR files: use the original transaction ID embedded in the request.
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>
        /// Gets the root path to the storage location.
        /// </summary>
        public string StorageRootPath { get; init; }

        /// <summary>
        /// Gets the full path to the instance.
        /// </summary>
        public string FilePath { get; init; }

        /// <summary>
        /// Gets a list of applications designated for the file.
        /// </summary>
        public string[] Applications { get; private set; }

        /// <summary>
        /// Gets or set the number of attempts to upload.
        /// </summary>
        public int TryCount { get; set; } = 0;

        public FileStorageInfo() { }

        public FileStorageInfo(string correlationId, string storageRootPath, string messageId, string fileExtension)
            : this(correlationId, storageRootPath, messageId, fileExtension, new FileSystem()) { }

        public FileStorageInfo(string correlationId, string storageRootPath, string messageId, string fileExtension, IFileSystem fileSystem)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(storageRootPath, nameof(storageRootPath));
            Guard.Against.NullOrWhiteSpace(messageId, nameof(messageId));
            Guard.Against.NullOrWhiteSpace(fileExtension, nameof(fileExtension));
            Guard.Against.Null(fileSystem, nameof(fileSystem));

            if (fileExtension[0] != '.')
            {
                fileExtension = $".{fileExtension}";
            }

            _fileSystem = fileSystem;
            CorrelationId = correlationId;
            StorageRootPath = storageRootPath;
            FilePath = GenerateStoragePath(storageRootPath, correlationId, messageId, fileExtension);
        }

        /// <summary>
        /// Application to be launched on MONAI Workload Manager, ignoring data routing agent.
        /// </summary>
        /// <param name="applications">List applications.</param>
        public void SetApplications(params string[] applications)
        {
            Guard.Against.NullOrEmpty(applications, nameof(applications));

            Applications = applications.Clone() as string[];
        }

        /// <summary>
        /// Generated the storage path for a file.
        /// </summary>
        /// <param name="storageRootPath">The directory path where the file stored.</param>
        /// <param name="correlationId">The correlation ID identifies the source of the file.  Use internally generated association ID for SCP received DICOM instances or Transaction ID for ACR retrieved files.</param>
        /// <param name="messageId">An unique identifier for the file.</param>
        /// <param name="fileExtension">File extension for the file.</param>
        /// <returns></returns>
        private string GenerateStoragePath(string storageRootPath, string correlationId, string messageId, string fileExtension)
        {
            string filePath = System.IO.Path.Combine(storageRootPath, $"{correlationId}-{messageId}") + fileExtension;
            filePath = filePath.ToLowerInvariant();
            var index = 1;
            while (_fileSystem.File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(storageRootPath, $"{correlationId}-{messageId}-{index++}") + fileExtension;
                filePath = filePath.ToLowerInvariant();
            }

            return filePath;
        }
    }
}
