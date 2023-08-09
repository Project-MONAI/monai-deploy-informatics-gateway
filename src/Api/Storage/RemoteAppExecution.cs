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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    public class RemoteAppExecution
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = default!;
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public string ExportTaskId { get; set; } = string.Empty;
        public string WorkflowInstanceId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? StudyUid { get; set; }
        public string? OutgoingUid { get { return Id; } set { Id = value ?? ""; } }
        public List<DestinationApplicationEntity> ExportDetails { get; set; } = new();
        public List<string> Files { get; set; } = new();
        public FileExportStatus Status { get; set; }
        public Dictionary<string, string> OriginalValues { get; set; } = new();
    }
}
