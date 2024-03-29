﻿/*
 * Copyright 2023 MONAI Consortium
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

using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Api.Models
{
    public class ExternalAppDetails : MongoDBEntityBase
    {
        public string StudyInstanceUid { get; set; } = string.Empty;

        public string StudyInstanceUidOutBound { get; set; } = string.Empty;

        public string WorkflowInstanceId { get; set; } = string.Empty;

        public string ExportTaskID { get; set; } = string.Empty;

        public string CorrelationId { get; set; } = string.Empty;

        public string? DestinationFolder { get; set; }

        public string PatientId { get; set; } = string.Empty;

        public string PatientIdOutBound { get; set; } = string.Empty;
    }
}
