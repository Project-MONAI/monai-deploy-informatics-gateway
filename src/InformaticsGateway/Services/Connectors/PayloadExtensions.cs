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

using System.Collections.Generic;
using System.Linq;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.Messaging.Common;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    internal static class PayloadExtensions
    {
        public static bool IsUploadComplete(this Payload payload)
        {
            return payload.Files.All(p => p.IsUploaded);
        }

        public static IReadOnlyList<string> GetWorkflows(this Payload payload)
        {
            return payload.Files.SelectMany(p => p.Workflows).Distinct().ToList();
        }

        public static IReadOnlyList<BlockStorageInfo> GetUploadedFiles(this Payload payload, string bucket)
        {
            Guard.Against.Null(bucket, nameof(bucket));

            return payload.Files.Select(p => new BlockStorageInfo
            {
                Path = p.UploadFilePath,
                Metadata = (p is DicomFileStorageInfo dicom) ? dicom.JsonUploadFilePath : null,
            }).ToList();
        }
    }
}
