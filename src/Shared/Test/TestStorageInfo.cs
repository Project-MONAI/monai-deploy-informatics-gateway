/*
 * Copyright 2022 MONAI Consortium
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

using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.SharedTest;

internal record TestStorageInfo : FileStorageInfo
{
    public TestStorageInfo(string filePath, string fileExtension)
        : base(fileExtension)
    {
        FilePath = filePath;
    }

    public TestStorageInfo(string filePath)
        : base(".test")
    {
        FilePath = filePath;
    }

    public override string UploadFilePath => $"/test/{FilePath}.test";

    protected override string SubDirectoryPath => "dir";
}
