// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
