// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public record FileStorageInfo
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
        /// Gets or sets the source of the file.
        /// </summary>
        public string Source { get; init; }

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
        /// Gets the file path to be stored on the shared storage.
        /// </summary>
        public string UploadPath
        {
            get
            {
                var path = FilePath[StorageRootPath.Length..];
                if (FileSystem.Path.IsPathRooted(path))
                {
                    path = path[1..];
                }
                return path;
            }
        }

        /// <summary>
        /// Gets the filename to be used on the shared storage.
        /// </summary>
        public string UploadFilename
        {
            get
            {
                return FileSystem.Path.GetFileName(FilePath);
            }
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

        /// <summary>
        /// Gets or sets the content type of the file.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the file and any associated meta files.
        /// </summary>
        public virtual IEnumerable<string> FilePaths
        {
            get
            {
                yield return _filePath;
            }
        }

        public FileStorageInfo() { }

        public FileStorageInfo(string correlationId,
                               string storageRootPath,
                               string messageId,
                               string fileExtension,
                               string source)
            : this(correlationId, storageRootPath, messageId, fileExtension, source, new FileSystem()) { }

        public FileStorageInfo(string correlationId,
                               string storageRootPath,
                               string messageId,
                               string fileExtension,
                               string source,
                               IFileSystem fileSystem)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(storageRootPath, nameof(storageRootPath));
            Guard.Against.NullOrWhiteSpace(messageId, nameof(messageId));
            Guard.Against.NullOrWhiteSpace(fileExtension, nameof(fileExtension));
            Guard.Against.NullOrWhiteSpace(source, nameof(source));

            Guard.Against.Null(fileSystem, nameof(fileSystem));

            if (fileExtension[0] != '.')
            {
                fileExtension = $".{fileExtension}";
            }

            FileSystem = fileSystem;
            MessageId = messageId;
            FileExtension = fileExtension;
            Source = source;

            Id = Guid.NewGuid();
            CorrelationId = correlationId;
            StorageRootPath = storageRootPath;
            Received = DateTime.UtcNow;
        }

        /// <summary>
        /// Workflows to be launched on MONAI Workflow Manager, ignoring data routing agent.
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
        public virtual BlockStorageInfo ToBlockStorageInfo(string bucket)
        {
            return new BlockStorageInfo
            {
                Bucket = bucket,
                Path = UploadPath,
            };
        }
    }
}
