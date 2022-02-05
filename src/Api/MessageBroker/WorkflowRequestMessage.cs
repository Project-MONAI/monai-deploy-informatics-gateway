// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Monai.Deploy.InformaticsGateway.Api.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public class WorkflowRequestMessage
    {
        /// <summary>
        /// Gets or sets the ID of the payload which is also used as the root path of the payload.
        /// </summary>
        [JsonProperty(PropertyName = "payload_id")]
        public Guid PayloadId { get; set; }

        /// <summary>
        /// Gets or sets the associated workflows to be launched.
        /// </summary>
        [JsonProperty(PropertyName = "workflows")]
        public IEnumerable<string> Workflows { get; set; }

        /// <summary>
        /// Gets or sets number of files in the payload.
        /// </summary>
        [JsonProperty(PropertyName = "file_count")]
        public int FileCount { get; set; }

        /// <summary>
        /// For DIMSE, the correlation ID is the UUID associated with the first DICOM association received. For an ACR inference request, the correlation ID is the Transaction ID in the original request.
        /// </summary>
        [JsonProperty(PropertyName = "correlation_id")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the time the data was received.
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a list of files and metadata files in this request.
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public List<BlockStorageInfo> Payload { get; } = new List<BlockStorageInfo>();
    }
}
