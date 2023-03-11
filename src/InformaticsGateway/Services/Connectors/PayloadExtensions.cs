/*
 * Copyright 2022-2023 MONAI Consortium
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
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.Messaging.Common;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    internal static class PayloadExtensions
    {
        public static bool IsUploadCompleted(this Payload payload)
        {
            return payload.Files.All(p => p.IsUploaded);
        }

        public static bool IsUploadCompletedWithFailures(this Payload payload)
        {
            return payload.Files.Count(p => p.IsUploadFailed) + payload.Files.Count(p => p.IsUploaded) == payload.Count; ;
        }

        public static bool IsMoveCompleted(this Payload payload)
        {
            return payload.Files.All(p => p.IsMoveCompleted);
        }

        public static IReadOnlyList<string> GetWorkflows(this Payload payload)
        {
            return payload.Files.SelectMany(p => p.Workflows).Distinct().ToList();
        }

        public static IReadOnlyList<BlockStorageInfo> GetUploadedFiles(this Payload payload)
        {
            return payload.Files.Select(p => new BlockStorageInfo
            {
                Path = p.File.UploadPath,
                Metadata = (p is DicomFileStorageMetadata dicom) ? dicom.JsonFile.UploadPath : null,
            }).ToList();
        }
    }
}
