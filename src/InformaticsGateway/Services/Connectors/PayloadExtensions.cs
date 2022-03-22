// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Linq;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Api.Storage;

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
