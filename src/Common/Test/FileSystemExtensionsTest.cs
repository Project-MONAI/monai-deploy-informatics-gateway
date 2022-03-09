// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Common.Test
{
    public class FileSystemExtensionsTest
    {
        [Fact]
        public void CreateDirectoryIfNotExists_ShallCreateDirectoryIfNotExists()
        {
            var dirToBeCreated = "/my/dir5";
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(false);
            fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>()));
            fileSystem.Object.Directory.CreateDirectoryIfNotExists(dirToBeCreated);

            fileSystem.Verify(p => p.Directory.Exists(It.IsAny<string>()), Times.Once());
            fileSystem.Verify(p => p.Directory.CreateDirectory(dirToBeCreated), Times.Once());
        }

        [Fact]
        public void CreateDirectoryIfNotExists_ShallNotCreateDirectoryIfExists()
        {
            var dirToBeCreated = "/my/dir5";
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(true);
            fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>()));
            fileSystem.Object.Directory.CreateDirectoryIfNotExists(dirToBeCreated);

            fileSystem.Verify(p => p.Directory.Exists(It.IsAny<string>()), Times.Once());
            fileSystem.Verify(p => p.Directory.CreateDirectory(dirToBeCreated), Times.Never());
        }

        [Fact]
        public void TryDelete_ReturnsTrueOnSuccessful()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "/src/"     , new MockDirectoryData() }
            });

            Assert.True(fileSystem.Directory.TryDelete("/src"));
        }

        [Fact]
        public void TryDelete_ReturnsFalseOnFailure()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
            });

            Assert.False(fileSystem.Directory.TryDelete("/src"));
        }

        [Fact]
        public void TryGenerateDirectory_ExceededRetries()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>())).Throws(new System.Exception());

            Assert.False(fileSystem.Object.Directory.TryGenerateDirectory("/some/path", out _));
        }

        [Fact]
        public void TryGenerateDirectory_GeneratesADirectory()
        {
            var fileSystem = new MockFileSystem();

            Assert.True(fileSystem.Directory.TryGenerateDirectory("/some/path", out string generatedPath));
            Assert.StartsWith("/some/path-", generatedPath);
        }
    }
}
