/*
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

using System.Text.Json.Serialization;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution
{
    public class RemoteAppExecution
    {
        /// <summary>
        /// Gets the ID of this record.
        /// </summary>
        [JsonPropertyName("_id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets the date time this record is created.
        /// </summary>
        public DateTimeOffset RequestTime { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the workflow instance ID of the original request.
        /// </summary>
        public string WorkflowInstanceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the export task ID of the original request.
        /// </summary>
        public string ExportTaskId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the correlation ID of the original request.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        ///// <summary>
        ///// Gets or sets the proxy value of Study Instance UID.
        ///// </summary>
        public string StudyInstanceUid { get; set; } = string.Empty;

        ///// <summary>
        ///// Gets or sets the proxy value of Series Instance UID.
        ///// </summary>
        public string SeriesInstanceUid { get; set; } = string.Empty;

        ///// <summary>
        ///// Gets or sets the proxy value of SOP Instance UID.
        ///// </summary>
        public string SopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original values of a given DICOM tag.
        /// </summary>
        public Dictionary<string, string> OriginalValues { get; init; } = new();

        public RemoteAppExecution()
        { }

        public RemoteAppExecution(ExportRequestDataMessage exportRequestDataMessage, string? studyInstanceUid, string? seriesInstanceUid)
        {
            WorkflowInstanceId = exportRequestDataMessage.WorkflowInstanceId;
            ExportTaskId = exportRequestDataMessage.ExportTaskId;
            CorrelationId = exportRequestDataMessage.CorrelationId;

            StudyInstanceUid = studyInstanceUid ?? Utilities.GetTagProxyValue<string>(DicomTag.StudyInstanceUID) ?? "";
            SeriesInstanceUid = seriesInstanceUid ?? Utilities.GetTagProxyValue<string>(DicomTag.SeriesInstanceUID) ?? "";
            SopInstanceUid = Utilities.GetTagProxyValue<string>(DicomTag.SOPInstanceUID) ?? "";
        }
    }
}
