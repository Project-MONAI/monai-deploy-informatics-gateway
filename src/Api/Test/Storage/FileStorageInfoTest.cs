// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Monai.Deploy.InformaticsGateway.SharedTest;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test
{
    public class FileStorageInfoTest
    {
        [Fact(DisplayName = "Shall prepend dot to file extension")]
        public void ShallPrependDotToFileExtension()
        {
            var fileStorageInfo = new TestStorageInfo("path/to/file.txt", "txt");

            Assert.Equal(".txt", fileStorageInfo.FileExtension);
        }
    }
}
