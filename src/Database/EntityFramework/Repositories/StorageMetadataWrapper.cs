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
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class StorageMetadataWrapper
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; }

        [JsonPropertyName("identity")]
        public string Identity { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; }

        [JsonPropertyName("isUploaded")]
        public bool IsUploaded { get; set; }

        private StorageMetadataWrapper()
        { }

        public StorageMetadataWrapper(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata);

            CorrelationId = metadata.CorrelationId;
            Identity = metadata.Id;
            Update(metadata);
        }

        public void Update(FileStorageMetadata metadata)
        {
            Guard.Against.Null(metadata);

            IsUploaded = metadata.IsUploaded;
            Value = JsonSerializer.Serialize<object>(metadata); // Must be <object> here

            if (metadata.GetType() is null || string.IsNullOrWhiteSpace(metadata.GetType().AssemblyQualifiedName))
            {
                throw new ArgumentException("Unable to determine the type", nameof(metadata));
            }
            TypeName = metadata.GetType().AssemblyQualifiedName!;
        }

        public FileStorageMetadata? GetObject()
        {
            var type = Type.GetType(TypeName, true);
            return JsonSerializer.Deserialize(Value, type) as FileStorageMetadata;
        }
    }
}
