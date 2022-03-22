// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Common
{
    internal record FileStoragePath
    {
        public string FilePath { get; set; }
    }

    internal record FhirStoragePath : FileStoragePath
    {
        public string ResourceType { get; set; }
        public string ResourceId { get; set; }
        public string Identifier { get => $"{ResourceType}/{ResourceId}"; }
    }

    internal record DicomStoragePaths : FileStoragePath
    {
        public StudySerieSopUids UIDs { get; set; }
        public string DicomMetadataFilePath { get; set; }
        public string Identifier { get => UIDs.Identifier; }
    }
}
