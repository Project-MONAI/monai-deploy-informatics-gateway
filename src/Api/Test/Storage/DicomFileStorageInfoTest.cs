// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class DicomFileStorageInfoTest
    {
        [Fact(DisplayName = "Shall set AE Titles")]
        public void ShallSetAeTitles()
        {
            var correlationId = Guid.NewGuid().ToString();
            var callingAeTitle = "calling";
            var calledAeTitle = "called";
            var fileStorageInfo = new DicomFileStorageInfo
            {
                CorrelationId = correlationId,
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop",
                Source = callingAeTitle,
                CalledAeTitle = calledAeTitle
            };

            Assert.Equal(callingAeTitle, fileStorageInfo.CallingAeTitle);
            Assert.Equal(calledAeTitle, fileStorageInfo.CalledAeTitle);
        }

        [Fact(DisplayName = "Shall return both DICOM file and JSON upload paths")]
        public void ShallReturUploadPaths()
        {
            var correlationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var root = $"{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}";
            var dcmPath = Path.Combine(root, "study", "series", "sop.dcm");
            var jsonPath = Path.Combine(root, "study", "series", "sop.dcm.json");

            var fileStorageInfo = new DicomFileStorageInfo
            {
                Source = transactionId,
                CorrelationId = correlationId,
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop",
                FilePath = dcmPath,
                JsonFilePath = jsonPath
            };

            Assert.Equal(
                $"{DicomFileStorageInfo.DicomSubDirectoryName}/{fileStorageInfo.StudyInstanceUid}/{fileStorageInfo.SeriesInstanceUid}/{fileStorageInfo.SopInstanceUid}{DicomFileStorageInfo.FilExtension}",
                fileStorageInfo.UploadFilePath);
            Assert.Equal(
                $"{DicomFileStorageInfo.DicomSubDirectoryName}/{fileStorageInfo.StudyInstanceUid}/{fileStorageInfo.SeriesInstanceUid}/{fileStorageInfo.SopInstanceUid}{DicomFileStorageInfo.FilExtension}{DicomFileStorageInfo.DicomJsonFileExtension}",
                fileStorageInfo.JsonUploadFilePath);

            Assert.Collection(fileStorageInfo.FilePaths,
                (path) => path.Equals(fileStorageInfo.UploadFilePath),
                (path) => path.Equals(fileStorageInfo.JsonUploadFilePath));
        }

        [Fact(DisplayName = "Shall return where data is stored for storage service")]
        public void ShallReturnBlockStorageInfo()
        {
            var correlationId = Guid.NewGuid().ToString();
            var fileStorageInfo = new DicomFileStorageInfo
            {
                CorrelationId = correlationId,
                StudyInstanceUid = "Study",
                SeriesInstanceUid = "Series",
                SopInstanceUid = "Sop"
            };

            var blockStorage = fileStorageInfo.ToBlockStorageInfo("bucket");
            Assert.Equal(fileStorageInfo.UploadFilePath, blockStorage.Path);
            Assert.Equal(fileStorageInfo.JsonUploadFilePath, blockStorage.Metadata);
        }
    }
}
