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

internal record TestStorageInfo : FileStorageMetadata
{
    public TestStorageInfo(string correlationsId, string identifier, string filePath, string fileExtension)
        : base(correlationsId, identifier)
    {
        File = new StorageObjectMetadata(fileExtension)
        {
            UploadPath = filePath,
            TemporaryPath = filePath
        };
    }

    public override string DataTypeDirectoryName => "dir";

    public override StorageObjectMetadata File { get; set; }

    public void SetUploaded()
    {
        File.SetUploaded("test");
    }
}
