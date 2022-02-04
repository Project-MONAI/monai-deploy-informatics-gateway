// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Monai.Deploy.InformaticsGateway.Api.Storage;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class FileStorageInfoTest
    {
        [Fact(DisplayName = "Shall prepend dot to file extension")]
        public void ShallPrependDotToFileExtension()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var fileStorageInfo = new FileStorageInfo(correlationId, root, messagId, "txt", transactionId);

            Assert.Equal($"{root}{correlationId}-{messagId}.txt", fileStorageInfo.FilePath);
            Assert.Equal($"{correlationId}-{messagId}.txt", fileStorageInfo.UploadPath);
            Assert.Equal($"{correlationId}-{messagId}.txt", fileStorageInfo.UploadFilename);
        }

        [Fact(DisplayName = "Shall prevent overwriting existing files")]
        public void ShallAppendRandomValueToPreventOverwritingExistingFiles()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var fileExtension = ".txt";
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile($"{root}{correlationId}-{messagId}{fileExtension}", new MockFileData("context"));
            var fileStorageInfo = new FileStorageInfo(correlationId, root, messagId, fileExtension, transactionId, mockFileSystem);

            Assert.NotEqual($"{root}{correlationId}-{messagId}{fileExtension}", fileStorageInfo.FilePath);
        }

        [Fact(DisplayName = "Shall copy workflows")]
        public void ShallCloneWorkflows()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var fileExtension = ".txt";
            var mockFileSystem = new MockFileSystem();
            var fileStorageInfo = new FileStorageInfo(correlationId, root, messagId, fileExtension, transactionId, mockFileSystem);

            var workflows = new List<string>() { "A", "B", "C" };
            fileStorageInfo.SetWorkflows(workflows.ToArray());

            workflows.Clear();
            Assert.Equal(3, fileStorageInfo.Workflows.Length);
            Assert.Collection(fileStorageInfo.Workflows,
                item => item.Equals("A"),
                item => item.Equals("B"),
                item => item.Equals("C"));
        }

        [Fact(DisplayName = "Shall remove root from upload path")]
        public void ShallRemoveRootFromUploadPath()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/test";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var fileStorageInfo = new FileStorageInfo(correlationId, root, messagId, "txt", transactionId);

            Assert.Equal($"{correlationId}-{messagId}.txt", fileStorageInfo.UploadPath);
            Assert.Equal($"{correlationId}-{messagId}.txt", fileStorageInfo.UploadFilename);
        }

        [Fact(DisplayName = "Shall return where data is stored for storage service")]
        public void ShallReturnBlockStorageInfo()
        {
            var correlationId = Guid.NewGuid().ToString();
            var root = "/test";
            var messagId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var fileStorageInfo = new FileStorageInfo(correlationId, root, messagId, "txt", transactionId);

            var blockStorage = fileStorageInfo.ToBlockStorageInfo("bucket");
            Assert.Equal("bucket", blockStorage.Bucket);
            Assert.Equal(fileStorageInfo.UploadPath, blockStorage.Path);
        }
    }
}
