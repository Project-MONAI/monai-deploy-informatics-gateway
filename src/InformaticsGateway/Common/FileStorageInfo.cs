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
using System;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Common
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    internal record FileStorageInfo
    {
        private string _filePath;
        protected string MessageId { get; init; }
        protected string FileExtension { get; init; }

        protected IFileSystem FileSystem { get; init; }

        /// <summary>
        /// Gets the unique ID of the file.
        /// </summary>
        public Guid Id { get; init; }

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
        public string FilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_filePath))
                {
                    _filePath = GenerateStoragePath();
                };
                return _filePath;
            }
            set => _filePath = value;
        }

        /// <summary>
        /// Gets a list of workflows designated for the file.
        /// </summary>
        public string[] Workflows { get; private set; }

        /// <summary>
        /// Gets or sets the DateTime that the file was received.
        /// </summary>
        /// <value></value>
        public DateTime Received { get; set; }

        /// <summary>
        /// Gets or set database row versioning info.
        /// </summary>
        /// <value></value>
        public byte[] Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the number of attempts to upload.
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

            FileSystem = fileSystem;
            MessageId = messageId;
            FileExtension = fileExtension;

            Id = Guid.NewGuid();
            CorrelationId = correlationId;
            StorageRootPath = storageRootPath;
            Received = DateTime.UtcNow;
        }

        /// <summary>
        /// Workflows to be launched on MONAI Workload Manager, ignoring data routing agent.
        /// </summary>
        /// <param name="workflows">List of workflows.</param>
        public void SetWorkflows(params string[] workflows)
        {
            Guard.Against.NullOrEmpty(workflows, nameof(workflows));

            Workflows = workflows.Clone() as string[];
        }

        /// <summary>
        /// Generated the storage path for a file.
        /// </summary>
        protected virtual string GenerateStoragePath()
        {
            string filePath = System.IO.Path.Combine(StorageRootPath, $"{CorrelationId}-{MessageId}") + FileExtension;
            filePath = filePath.ToLowerInvariant();
            var index = 1;
            while (FileSystem.File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(StorageRootPath, $"{CorrelationId}-{MessageId}-{index++}") + FileExtension;
                filePath = filePath.ToLowerInvariant();
            }

            return filePath;
        }
    }
}
