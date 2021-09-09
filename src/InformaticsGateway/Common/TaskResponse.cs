/*
 * Apache License, Version 2.0
 * Copyright 2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text.Json.Serialization;

//TODO: remove this file and replace with lib from MWM
namespace Monai.Deploy.InformaticsGateway.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum State
    {
        All = 0,
        Pending = 1,
        InProgress,
        Succeeded,
        Failed
    }

    public class TaskResponse
    {
        public Guid ExportTaskId { get; set; }
        public Guid FileId { get; set; }
        public string CorrelationId { get; set; }
        public string ApplicationId { get; set; }
        public string Sink { get; set; }
        public string Parameters { get; set; }
        public State State { get; set; }
        public DateTime DateCompleted { get; set; }
        public int Retries { get; set; }
    }
}
