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

using System;
using System.Collections.Generic;
using System.Linq;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public class ExportRequestEventDetails : ExportRequestEvent
    {
        public ExportRequestEventDetails(ExportRequestEvent exportRequest)
        {
            CorrelationId = exportRequest.CorrelationId;
            ExportTaskId = exportRequest.ExportTaskId;
            Files = new List<string>(exportRequest.Files);
            Destinations = new string[exportRequest.Destinations.Length];
            Array.Copy(exportRequest.Destinations, Destinations, exportRequest.Destinations.Length);
            exportRequest.Destinations.CopyTo(Destinations, 0);
            DeliveryTag = exportRequest.DeliveryTag;
            MessageId = exportRequest.MessageId;
            WorkflowInstanceId = exportRequest.WorkflowInstanceId;

            PluginAssemblies.AddRange(exportRequest.PluginAssemblies);
            ErrorMessages.AddRange(exportRequest.ErrorMessages);

            StartTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the time the export request received.
        /// </summary>
        public DateTimeOffset StartTime { get; }


        /// <summary>
        /// Gets time between now and <see cref="StartTime"/>.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return DateTimeOffset.UtcNow.Subtract(StartTime);
            }
        }

        /// <summary>
        /// Gets or set number of files exported successfully.
        /// </summary>
        public int SucceededFiles { get; set; } = 0;

        /// <summary>
        /// Gets or sets number of files failed to export.
        /// </summary>
        public int FailedFiles { get; set; } = 0;

        /// <summary>
        /// Gets whether the export task is completed or not based on file count.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return (SucceededFiles + FailedFiles) == Files.Count();
            }
        }

        public Dictionary<string, FileExportStatus> FileStatuses { get; private set; } = new Dictionary<string, FileExportStatus>();

        public ExportStatus Status
        {
            get
            {
                if (SucceededFiles == Files.Count())
                {
                    return ExportStatus.Success;
                }
                else if (FailedFiles == Files.Count())
                {
                    return ExportStatus.Failure;
                }
                else if (SucceededFiles > 0 && FailedFiles > 0)
                {
                    return ExportStatus.PartialFailure;
                }
                return ExportStatus.Unknown;
            }
        }
    }
}
