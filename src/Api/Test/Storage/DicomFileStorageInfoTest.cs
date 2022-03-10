// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class DicomFileStorageInfoTest
    {
        [Fact(DisplayName = "DicomFileStorageInfo - Shall prevent overwriting existing file")]
        public void ShallAppendRandomValueToPreventOverwritingExistingFiles()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var mockFileSystem = new MockFileSystem();
            var fileStorageInfo = new DicomFileStorageInfo(correlationId, root, messagId, transactionId, mockFileSystem)
            {
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop"
            };
            var existingFilePath = Path.Combine(
                    root,
                    fileStorageInfo.StudyInstanceUid,
                    fileStorageInfo.SeriesInstanceUid,
                    fileStorageInfo.SopInstanceUid) + ".dcm";
            mockFileSystem.AddFile(existingFilePath, new MockFileData("context"));
            Assert.NotEqual(existingFilePath, fileStorageInfo.FilePath);
        }

        [Fact(DisplayName = "Shall return both DICOM file and JSON file paths")]
        public void ShallReturAllFilePaths()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/test";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var mockFileSystem = new MockFileSystem();
            var fileStorageInfo = new DicomFileStorageInfo(correlationId, root, messagId, transactionId, mockFileSystem)
            {
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop"
            };

            Assert.Collection(fileStorageInfo.FilePaths,
                (path) => path.Equals(fileStorageInfo.FilePath),
                (path) => path.Equals(fileStorageInfo.DicomJsonFilePath));
        }

        [Fact(DisplayName = "Shall return JSON file info")]
        public void ShallReturJsonFileInfo()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/test";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var mockFileSystem = new MockFileSystem();
            var fileStorageInfo = new DicomFileStorageInfo(correlationId, root, messagId, transactionId, mockFileSystem)
            {
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop"
            };

            Assert.Equal($"{Path.Combine(fileStorageInfo.StudyInstanceUid, fileStorageInfo.SeriesInstanceUid, fileStorageInfo.SopInstanceUid).ToLowerInvariant()}.dcm.json", fileStorageInfo.DicomJsonUploadPath);
        }

        [Fact(DisplayName = "Shall return where data is stored for storage service")]
        public void ShallReturnBlockStorageInfo()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/test";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var mockFileSystem = new MockFileSystem();
            var fileStorageInfo = new DicomFileStorageInfo(correlationId, root, messagId, transactionId, mockFileSystem)
            {
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop"
            };

            var blockStorage = fileStorageInfo.ToBlockStorageInfo("bucket");
            Assert.Equal("bucket", blockStorage.Bucket);
            Assert.Equal(fileStorageInfo.UploadPath, blockStorage.Path);
            Assert.Equal(fileStorageInfo.DicomJsonUploadPath, blockStorage.Metadata);
        }
    }
}
