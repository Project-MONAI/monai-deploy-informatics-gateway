// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Common
{
    public class DicomFileStorageInfoTest
    {
        [Fact(DisplayName = "DicomFileStorageInfo - Shall prevent overwriting existing file")]
        public void ShallAppendRandomValueToPreventOverwritingExistingFiles()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/";
            var messagId = Guid.NewGuid().ToString();
            var mockFileSystem = new MockFileSystem();
            var fileStorageInfo = new DicomFileStorageInfo(correlationId, root, messagId, mockFileSystem);

            fileStorageInfo.StudyInstanceUid = "Study";
            fileStorageInfo.SeriesInstanceUid = "Series";
            fileStorageInfo.SopInstanceUid = "Sop";
            var existingFilePath = Path.Combine(
                    root,
                    fileStorageInfo.StudyInstanceUid,
                    fileStorageInfo.SeriesInstanceUid,
                    fileStorageInfo.SopInstanceUid) + ".dcm";
            mockFileSystem.AddFile(existingFilePath, new MockFileData("context"));
            Assert.NotEqual(existingFilePath, fileStorageInfo.FilePath);
        }
    }
}
