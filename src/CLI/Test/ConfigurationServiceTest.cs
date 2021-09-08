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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ConfigurationServiceTest
    {
        private readonly Mock<ILogger<ConfigurationService>> _logger;
        private readonly Mock<IFileSystem> _fileSystem;

        public ConfigurationServiceTest()
        {
            _logger = new Mock<ILogger<ConfigurationService>>();
            _fileSystem = new Mock<IFileSystem>();
        }

        [Fact(DisplayName = "CreateConfigDirectoryIfNotExist creates directory")]
        public void CreateConfigDirectoryIfNotExist_CreateDirectory()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);

            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(false);
            svc.CreateConfigDirectoryIfNotExist();

            _fileSystem.Verify(p => p.Directory.CreateDirectory(It.IsAny<string>()), Times.Once());
        }

        [Fact(DisplayName = "CreateConfigDirectoryIfNotExist skips creating directory")]
        public void CreateConfigDirectoryIfNotExist_SkipsCreation()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);

            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(true);
            svc.CreateConfigDirectoryIfNotExist();

            _fileSystem.Verify(p => p.Directory.CreateDirectory(It.IsAny<string>()), Times.Never());
        }

        [Fact(DisplayName = "ConfigurationExists")]
        public void ConfigurationExists()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);

            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(true);
            Assert.True(svc.ConfigurationExists());
            _fileSystem.Setup(p => p.File.Exists(It.IsAny<string>())).Returns(false);
            Assert.False(svc.ConfigurationExists());
        }

        [Fact(DisplayName = "Load verbose logging")]
        public void Load_Verbose()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);

            var stream = new System.IO.StringWriter();
            var config = new ConfigurationOptions { Endpoint = "http://test" };
            var json = JsonConvert.SerializeObject(config);
            _fileSystem.Setup(p => p.File.OpenText(It.IsAny<string>())).Returns(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json))));

            var result = svc.Load(true);

            Assert.Equal(config.Endpoint, result.Endpoint);
            _logger.VerifyLogging(LogLevel.Debug, Times.AtLeastOnce());
        }

        [Fact(DisplayName = "Load no verbose logging")]
        public void Load_NoVerbose()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);

            var stream = new System.IO.StringWriter();
            var config = new ConfigurationOptions { Endpoint = "http://test" };
            var json = JsonConvert.SerializeObject(config);
            _fileSystem.Setup(p => p.File.OpenText(It.IsAny<string>())).Returns(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json))));

            var result = svc.Load();

            Assert.Equal(config.Endpoint, result.Endpoint);
            _logger.VerifyLogging(LogLevel.Debug, Times.Never());
        }

        [Fact(DisplayName = "Load throws")]
        public void Load_Throws()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);

            using var stream = new System.IO.StringWriter();
            _fileSystem.Setup(p => p.File.OpenText(It.IsAny<string>())).Throws(new Exception("error"));

            var result = svc.Load();

            Assert.NotNull(result);
            Assert.Null(result.Endpoint);
            _logger.VerifyLogging(LogLevel.Warning, Times.Once());
            _logger.VerifyLogging(LogLevel.Debug, Times.Never());
        }

        [Fact(DisplayName = "Save")]
        public void Save()
        {
            var svc = new ConfigurationService(_logger.Object, _fileSystem.Object);
            var config = new ConfigurationOptions { Endpoint = "http://test" };

            byte[] bytes = null;
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    _fileSystem.Setup(p => p.File.CreateText(It.IsAny<string>())).Returns(sw);
                    svc.Save(config);
                    bytes = ms.ToArray();
                }
            }

            ConfigurationOptions result;
            using (var ms = new MemoryStream(bytes))
            {
                var serializer = new JsonSerializer();
                using var streamReader = new StreamReader(ms);
                result = serializer.Deserialize(streamReader, typeof(ConfigurationOptions)) as ConfigurationOptions;
            }
            Assert.NotNull(result);
            Assert.Equal(config.Endpoint, result.Endpoint);
        }
    }
}
