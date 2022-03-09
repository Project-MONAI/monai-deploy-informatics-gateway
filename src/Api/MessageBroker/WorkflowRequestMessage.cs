// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Newtonsoft.Json;

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
