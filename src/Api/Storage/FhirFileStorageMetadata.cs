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
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a FHIR resource and storage hierarchy/path.
    /// </summary>
    public sealed record FhirFileStorageMetadata : FileStorageMetadata
    {
        public const string FhirSubDirectoryName = "ehr";
        public const string JsonFilExtension = ".json";
        public const string XmlFilExtension = ".xml";

        /// <summary>
        /// The transaction ID of the original ACR request.
        /// Note: this value is same as <seealso cref="Source"></c>
        /// </summary>
        [JsonIgnore]
        public string TransactionId { get => Source; }

        /// <summary>
        /// Gets or set the FHIR resource type.
        /// </summary>
        [JsonPropertyName("resourceType")]
        public string ResourceType { get; set; } = default!;

        /// <summary>
        /// Gets or set the FHIR resource ID.
        /// </summary>
        [JsonPropertyName("resourceId")]
        public string ResourceId { get; set; } = default!;

        /// <inheritdoc/>
        [JsonIgnore]
        public override string DataTypeDirectoryName => FhirSubDirectoryName;

        /// <inheritdoc/>
        [JsonPropertyName("file")]
        public override StorageObjectMetadata File { get; set; } = default!;

        /// <summary>
        /// DO NOT USE
        /// This constructor is intended for JSON serializer.
        /// Due to limitation in current version of .NET, the constructor must be public.
        /// https://github.com/dotnet/runtime/issues/31511
        /// </summary>
        [JsonConstructor]
        public FhirFileStorageMetadata() { }

        public FhirFileStorageMetadata(string transactionId, string resourceType, string resourceId, FhirStorageFormat fhirFileFormat)
            : base(transactionId, $"{resourceType}{PathSeparator}{resourceId}")
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            Guard.Against.NullOrWhiteSpace(resourceType, nameof(resourceType));
            Guard.Against.NullOrWhiteSpace(resourceId, nameof(resourceId));

            Source = transactionId;
            ResourceType = resourceType;
            ResourceId = resourceId;

            var fileExtension = fhirFileFormat == FhirStorageFormat.Json ? JsonFilExtension : XmlFilExtension;
            File = new StorageObjectMetadata(fileExtension)
            {
                TemporaryPath = string.Join(PathSeparator, transactionId, DataTypeDirectoryName, $"{Guid.NewGuid()}{fileExtension}"),
                UploadPath = string.Join(PathSeparator, DataTypeDirectoryName, ResourceType, $"{ResourceId}{fileExtension}"),
                ContentType = fhirFileFormat == FhirStorageFormat.Json ? System.Net.Mime.MediaTypeNames.Application.Json : System.Net.Mime.MediaTypeNames.Application.Xml,
            };
        }
    }
}
