// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using Ardalis.GuardClauses;
using Monai.Deploy.Messaging.Messages;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public class ExportRequestDataMessage
    {
        private readonly ExportRequestMessage _exportRequest;

        public byte[] FileContent { get; private set; }
        public bool IsFailed { get; private set; }
        public IList<string> Messages { get; init; }

        public string ExportTaskId
        {
            get { return _exportRequest.ExportTaskId; }
        }

        public string CorrelationId
        {
            get { return _exportRequest.CorrelationId; }
        }

        public string Destination
        {
            get { return _exportRequest.Destination; }
        }

        public string Filename { get; }

        public ExportRequestDataMessage(ExportRequestMessage exportRequest, string filename)
        {
            IsFailed = false;
            Messages = new List<string>();

            _exportRequest = exportRequest ?? throw new System.ArgumentNullException(nameof(exportRequest));
            Filename = filename ?? throw new System.ArgumentNullException(nameof(filename));
        }

        public void SetData(byte[] data)
        {
            Guard.Against.Null(data, nameof(data));
            FileContent = data;
        }

        public void SetFailed(string errorMessage)
        {
            Guard.Against.NullOrWhiteSpace(errorMessage, nameof(errorMessage));
            IsFailed = true;
            Messages.Add(errorMessage);
        }
    }
}
