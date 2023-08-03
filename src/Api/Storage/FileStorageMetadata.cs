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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public abstract record FileStorageMetadata
    {
        internal const char PathSeparator = '/';

        /// <summary>
        /// Gets the root directory name
        /// </summary>
        public abstract string DataTypeDirectoryName { get; }

        /// <summary>
        /// Gets the storage object associated.
        /// </summary>
        [JsonPropertyName("file")]
        public abstract StorageObjectMetadata File { get; set; }

        [JsonIgnore]
        public virtual bool IsUploaded { get { return File.IsUploaded; } }

        [JsonIgnore]
        public virtual bool IsUploadFailed { get { return File.IsUploadFailed; } }

        [JsonIgnore]
        public virtual bool IsMoveCompleted { get { return File.IsMoveCompleted; } }

        /// <summary>
        /// Gets the unique (user-defined) ID of the file.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; init; } = default!;

        /// <summary>
        /// Gets the correlation ID of the file.
        /// For SCP received DICOM instances: use internally generated unique association ID.
        /// For ACR retrieved DICOM/FHIR files: use the original transaction ID embedded in the request.
        /// </summary>
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; init; } = default!;

        /// <summary>
        /// Gets or sets the source of the file.
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = default!;

        /// <summary>
        /// Gets a list of workflows designated for the file.
        /// </summary>
        [JsonPropertyName("workflows"), JsonInclude]
        public List<string> Workflows { get; private set; } = default!;

        /// <summary>
        /// Gets or sets the DateTime that the file was received.
        /// </summary>
        /// <value></value>
        [JsonPropertyName("dateReceived")]
        public DateTime DateReceived { get; init; } = default!;

        /// <summary>
        /// DO NOT USE
        /// This constructor is intended for JSON serializer.
        /// Due to limitation in current version of .NET, the constructor must be public.
        /// https://github.com/dotnet/runtime/issues/31511
        /// </summary>
        [JsonConstructor]
        protected FileStorageMetadata() { }

        protected FileStorageMetadata(string correlationId, string identifier)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));

            CorrelationId = correlationId;
            Id = identifier;
            DateReceived = DateTime.UtcNow;
            Workflows = new List<string>();
        }

        /// <summary>
        /// Workflows to be launched on MONAI Workflow Manager, ignoring data routing agent.
        /// </summary>
        /// <param name="workflows">List of workflows.</param>
        public void SetWorkflows(params string[] workflows)
        {
            Guard.Against.NullOrEmpty(workflows, nameof(workflows));

            Workflows.AddRange(workflows);
        }

        public virtual void SetFailed()
        {
            File.SetFailed();
        }

        public string? PayloadId { get; set; }
    }
}
