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
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Api.Storage
{
    /// <summary>
    /// Provides basic information for a DICOM instance and storage hierarchy/path.
    /// </summary>
    public sealed record DicomFileStorageInfo : FileStorageInfo
    {
        public static readonly string FilExtension = ".dcm";
        public static readonly string DicomJsonFileExtension = ".json";
        public static readonly string DicomContentType = "application/dicom";
        public static readonly string DicomJsonContentType = "application/json";

        public string StudyInstanceUid { get; set; }
        public string SeriesInstanceUid { get; set; }
        public string SopInstanceUid { get; set; }
        public string DicomJsonFilePath { get; set; }

        /// <summary>
        /// Gets the file path to be stored on the shared storage.
        /// </summary>
        public string DicomJsonUploadPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    _ = FilePath;
                };

                var path = DicomJsonFilePath[StorageRootPath.Length..];
                if (FileSystem.Path.IsPathRooted(path))
                {
                    return path[1..];
                }
                return path;
            }
        }

        public override IEnumerable<string> FilePaths
        {
            get
            {
                yield return FilePath;
                yield return DicomJsonFilePath;
            }
        }
        

        public DicomFileStorageInfo() { }

        public DicomFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    string source)
            : base(correlationId, storageRootPath, messageId, FilExtension, source, new FileSystem())
        {
            ContentType = DicomContentType;
        }

        public DicomFileStorageInfo(string correlationId,
                                    string storageRootPath,
                                    string messageId,
                                    string source,
                                    IFileSystem fileSystem)
            : base(correlationId, storageRootPath, messageId, FilExtension, source, fileSystem)
        {
            ContentType = DicomContentType;
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

            DicomJsonFilePath = $"{filePath}{DicomJsonFileExtension}";
            return filePath;
        }

        public override BlockStorageInfo ToBlockStorageInfo(string bucket)
        {
            var blockStorage = base.ToBlockStorageInfo(bucket);
            blockStorage.Metadata = DicomJsonUploadPath;
            return blockStorage;
        }
    }
}
