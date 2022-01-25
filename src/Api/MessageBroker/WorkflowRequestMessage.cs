// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public class WorkflowRequestMessage
    {
        /// <summary>
        /// Gets or sets the name of bucket where the payload is stored.
        /// </summary>
        public string Bucket { get; set; }

        /// <summary>
        /// Gets or sets the ID of the payload which is also used as the root path of the payload.
        /// </summary>
        public Guid PayloadId { get; set; }

        /// <summary>
        /// Gets or sets the associated workflows to be launched.
        /// </summary>
        public IEnumerable<string> Workflows { get; set; }

        /// <summary>
        /// Gets or sets number of files in the payload.
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// For DIMSE, the correlation ID is the UUID associated with the first DICOM association received. For an ACR inference request, the correlation ID is the Transaction ID in the original request.
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
