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
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a FHIR resource and storage hierarchy/path.
    /// </summary>
    public sealed record Hl7FileStorageMetadata : FileStorageMetadata
    {
        public const string Hl7SubDirectoryName = "ehr";
        public const string FileExtension = ".txt";

        /// <inheritdoc/>
        [JsonIgnore]
        public override string DataTypeDirectoryName => Hl7SubDirectoryName;

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
        public Hl7FileStorageMetadata() { }

        public Hl7FileStorageMetadata(string connectionId, DataService dataType, string dataOrigin)
            : base(connectionId, Guid.NewGuid().ToString())
        {
            Guard.Against.NullOrWhiteSpace(connectionId, nameof(connectionId));

            DataOrigin.DataService = dataType;
            DataOrigin.Source = dataOrigin;
            DataOrigin.Destination = IpAddress();

            File = new StorageObjectMetadata(FileExtension)
            {
                TemporaryPath = string.Join(PathSeparator, connectionId, DataTypeDirectoryName, $"{base.Id}{FileExtension}"),
                UploadPath = string.Join(PathSeparator, DataTypeDirectoryName, $"{base.Id}{FileExtension}"),
                ContentType = System.Net.Mime.MediaTypeNames.Text.Plain,
            };
        }
    }
}