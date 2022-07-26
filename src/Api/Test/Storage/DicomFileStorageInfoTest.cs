/*
 * Copyright 2021-2022 MONAI Consortium
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
