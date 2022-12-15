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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ConfigurationServiceTest
    {
        private readonly Mock<ILogger<ConfigurationService>> _logger;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IEmbeddedResource> _embeddedResource;

        public ConfigurationServiceTest()
        {
            _logger = new Mock<ILogger<ConfigurationService>>();
            _fileSystem = new Mock<IFileSystem>();
            _embeddedResource = new Mock<IEmbeddedResource>();
        }

        [Fact(DisplayName = "ConfigurationServiceTest constructor")]
        public void ConfigurationServiceTest_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigurationService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ConfigurationService(_logger.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new ConfigurationService(_logger.Object, _fileSystem.Object, null));

            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object, _embeddedResource.Object);
            Assert.NotNull(svc.Configurations);
        }

        [Fact(DisplayName = "CreateConfigDirectoryIfNotExist creates directory")]
        public void CreateConfigDirectoryIfNotExist_CreateDirectory()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object, _embeddedResource.Object);

            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(false);
            svc.CreateConfigDirectoryIfNotExist();

            _fileSystem.Verify(p => p.Directory.CreateDirectory(It.IsAny<string>()), Times.Once());
        }

        [Fact(DisplayName = "CreateConfigDirectoryIfNotExist skips creating directory")]
        public void CreateConfigDirectoryIfNotExist_SkipsCreation()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object, _embeddedResource.Object);

            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(true);
            svc.CreateConfigDirectoryIfNotExist();

            _fileSystem.Verify(p => p.Directory.CreateDirectory(It.IsAny<string>()), Times.Never());
        }

        [Fact(DisplayName = "IsInitialized")]
        public void IsInitialized()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object, _embeddedResource.Object);

            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(true);
            Assert.True(svc.IsInitialized);
            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(false);
            Assert.False(svc.IsInitialized);
        }

        [Fact(DisplayName = "IsConfigExists")]
        public void ConfigurationExists()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object, _embeddedResource.Object);

            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(true);
            Assert.True(svc.IsConfigExists);
            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(false);
            Assert.False(svc.IsConfigExists);
        }

        [Fact(DisplayName = "Initialize with missing config resource")]
        public async Task Initialize_ShallThrowWhenConfigReousrceIsMissing()
        {
            _embeddedResource.Setup(p => p.GetManifestResourceStream(It.IsAny<string>())).Returns(default(Stream));

            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object, _embeddedResource.Object);
            await Assert.ThrowsAsync<ConfigurationException>(async () => await svc.Initialize(CancellationToken.None));
        }

        [Fact(DisplayName = "Initialize creates the config file")]
        public async Task Initialize_CreatesTheConfigFile()
        {
            var fileSystem = new MockFileSystem();
            var testString = "hello world";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(testString));
            var mockSteam = new Mock<Stream>();

            _embeddedResource.Setup(p => p.GetManifestResourceStream(It.IsAny<string>())).Returns(memoryStream);

            var svc = new ConfigurationService(_logger.Object, fileSystem, _embeddedResource.Object);
            await svc.Initialize(CancellationToken.None);

            _embeddedResource.Verify(p => p.GetManifestResourceStream(Common.AppSettingsResourceName), Times.Once());

            var bytesWritten = await fileSystem.File.ReadAllBytesAsync(Common.ConfigFilePath).ConfigureAwait(false);
            Assert.Equal(testString, Encoding.UTF8.GetString(bytesWritten));
        }
    }
}
