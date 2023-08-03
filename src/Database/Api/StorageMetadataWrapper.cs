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

using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.Api
{
    /// <summary>
    /// A wrapper class to support polymorphic types of <see cref="FileStorageMetadata" />
    /// </summary>
    public class StorageMetadataWrapper : MongoDBEntityBase
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; } = string.Empty;

        [JsonPropertyName("identity")]
        public string Identity { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("isUploaded")]
        public bool IsUploaded { get; set; }

        private StorageMetadataWrapper()
        { }

        public StorageMetadataWrapper(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            CorrelationId = metadata.CorrelationId;
            Identity = metadata.Id;
            Update(metadata);
        }

        public void Update(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            IsUploaded = metadata.IsUploaded;
            Value = JsonSerializer.Serialize<object>(metadata); // Must be <object> here

            if (metadata.GetType() is null || string.IsNullOrWhiteSpace(metadata.GetType().AssemblyQualifiedName))
            {
                throw new ArgumentException("Unable to determine the type", nameof(metadata));
            }
            TypeName = metadata.GetType().AssemblyQualifiedName!;
        }

        public FileStorageMetadata GetObject()
        {
            var type = Type.GetType(TypeName, true);

            if (type is null)
            {
                throw new NotSupportedException($"Unable to locate type {TypeName} in the current application.");
            }

#pragma warning disable CS8603 // Possible null reference return.
            return JsonSerializer.Deserialize(Value, type) as FileStorageMetadata;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public override string ToString()
        {
            return $"Identity: {Identity}";
        }
    }
}
