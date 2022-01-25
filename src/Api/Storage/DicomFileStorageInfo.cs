// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public sealed record DicomFileStorageInfo : FileStorageInfo
    {
        public static readonly string FILE_EXTENSION = ".dcm";
        public static readonly string CONTENT_TYPE = "application/dicom";
        public string StudyInstanceUid { get; set; }
        public string SeriesInstanceUid { get; set; }
        public string SopInstanceUid { get; set; }

        public DicomFileStorageInfo() { }

        public DicomFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    string source)
            : base(correlationId, storageRootPath, messageId, FILE_EXTENSION, source, new FileSystem())
        {
            ContentType = CONTENT_TYPE;
        }

        public DicomFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    string source,
                                    IFileSystem fileSystem)
            : base(correlationId, storageRootPath, messageId, FILE_EXTENSION, source, fileSystem)
        {
            ContentType = CONTENT_TYPE;
        }

        protected override string GenerateStoragePath()
        {
            Guard.Against.NullOrWhiteSpace(StudyInstanceUid, nameof(StudyInstanceUid));
            Guard.Against.NullOrWhiteSpace(SeriesInstanceUid, nameof(SeriesInstanceUid));
            Guard.Against.NullOrWhiteSpace(SopInstanceUid, nameof(SopInstanceUid));

            string filePath = System.IO.Path.Combine(StorageRootPath, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid) + FileExtension;
            filePath = filePath.ToLowerInvariant();
            var index = 1;
            while (FileSystem.File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(StorageRootPath, StudyInstanceUid, SeriesInstanceUid, $"{SopInstanceUid}-{index++}") + FileExtension;
                filePath = filePath.ToLowerInvariant();
            }

            return filePath;
        }
    }
}
