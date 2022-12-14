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

namespace Monai.Deploy.InformaticsGateway.Api
{
    public class DicomAssociationInfo : MongoDBEntityBase
    {
        public DateTime DateTimeDisconnected { get; set; } = default!;
        public string CorrelationId { get; set; } = default!;
        public int FileCount { get; private set; }
        public string CallingAeTitle { get; set; } = default!;
        public string CalledAeTitle { get; set; } = default!;
        public string RemoteHost { get; set; } = default!;
        public int RemotePort { get; set; } = default!;
        public string Errors { get; set; } = string.Empty;
        public TimeSpan Duration { get; private set; } = default!;

        public DicomAssociationInfo()
        {
            FileCount = 0;
        }

        public void FileReceived()
        {
            FileCount++;
        }

        public void Disconnect()
        {
            DateTimeDisconnected = DateTime.UtcNow;
            Duration = DateTimeDisconnected.Subtract(DateTimeCreated);
        }
    }
}
