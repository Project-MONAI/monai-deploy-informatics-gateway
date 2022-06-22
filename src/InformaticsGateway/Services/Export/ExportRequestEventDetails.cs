// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
            AddErrorMessages(exportRequest.ErrorMessages);
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
        { get { return (SucceededFiles + FailedFiles) == Files.Count(); } }

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
