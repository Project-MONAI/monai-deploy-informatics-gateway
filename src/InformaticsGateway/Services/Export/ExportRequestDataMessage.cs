/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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

using System.Collections.Generic;
using Ardalis.GuardClauses;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public class ExportRequestDataMessage
    {
        private readonly ExportRequestEvent _exportRequest;

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

        public string[] Destinations
        {
            get { return _exportRequest.Destinations; }
        }

        public string Filename { get; }

        public ExportRequestDataMessage(ExportRequestEvent exportRequest, string filename)
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
