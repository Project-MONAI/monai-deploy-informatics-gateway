// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO.Abstractions;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a FHIR resource and storage hierarchy/path.
    /// </summary>
    public sealed record FhirFileStorageInfo : FileStorageInfo
    {
        public static readonly string JsonFilExtension = ".json";
        public static readonly string XmlFilExtension = ".xml";
        private static readonly string DirectoryPath = "ehr";

        public string ResourceType { get; set; }

        public FhirFileStorageInfo() { }

        public FhirFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    FhirStorageFormat fhirFileFormat,
                                    string source)
            : base(correlationId, storageRootPath, messageId, fhirFileFormat == FhirStorageFormat.Json ? JsonFilExtension : XmlFilExtension, source, new FileSystem()) { }

        public FhirFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    FhirStorageFormat fhirFileFormat,
                                    string source,
                                    IFileSystem fileSystem)
            : base(correlationId, storageRootPath, messageId, fhirFileFormat == FhirStorageFormat.Json ? JsonFilExtension : XmlFilExtension, source, fileSystem)
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
