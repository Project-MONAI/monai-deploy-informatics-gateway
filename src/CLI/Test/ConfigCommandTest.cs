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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ConfigCommandTest

    {
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;

        public ConfigCommandTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger>();
            _configurationService = new Mock<IConfigurationService>();
            _commandLineBuilder = new CommandLineBuilder()
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureServices(services =>
                        {
                            services.AddSingleton<ILoggerFactory>(p => _loggerFactory.Object);
                            services.AddSingleton<IConfigurationService>(p => _configurationService.Object);
                        });
                    })
                .AddCommand(new ConfigCommand());
            _paser = _commandLineBuilder.Build();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
        }

        [Fact(DisplayName = "config comand")]
        public async Task Config_Command()
        {
            var command = "config";
            var result = _paser.Parse(command);
            Assert.Equal("Option '-e' is required.", result.Errors.First().Message);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);
        }

        [Fact(DisplayName = "config show comand when not yet configured")]
        public async Task ConfigShow_Command_NotConfigured()
        {
            var command = "config show";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.Setup(p => p.ConfigurationExists()).Returns(false);
            _configurationService.Setup(p => p.Load(It.IsAny<bool>())).Returns(new ConfigurationOptions { Endpoint = "http://test" });

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _configurationService.Verify(p => p.ConfigurationExists(), Times.Once());
            _configurationService.Verify(p => p.Load(It.IsAny<bool>()), Times.Never());
        }

        [Fact(DisplayName = "config show comand ")]
        public async Task ConfigShow_Command()
        {
            var command = "config show";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.Setup(p => p.ConfigurationExists()).Returns(true);
            _configurationService.Setup(p => p.Load(It.IsAny<bool>())).Returns(new ConfigurationOptions { Endpoint = "http://test" });

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _configurationService.Verify(p => p.ConfigurationExists(), Times.Once());
            _configurationService.Verify(p => p.Load(It.IsAny<bool>()), Times.Once());

            _logger.VerifyLogging("Endpoint: http://test", LogLevel.Information, Times.Once());
        }

        [Fact(DisplayName = "config with options")]
        public async Task Config_Command_WithOptions()
        {
            var command = "config -e http://new";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.Setup(p => p.Load(It.IsAny<bool>())).Returns(new ConfigurationOptions { Endpoint = "http://old" });
            _configurationService.Setup(p => p.CreateConfigDirectoryIfNotExist());

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _configurationService.Verify(p => p.Load(It.IsAny<bool>()), Times.Once());
            _configurationService.Verify(p => p.CreateConfigDirectoryIfNotExist(), Times.Once());
            _configurationService.Verify(p => p.Save(It.Is<ConfigurationOptions>(
                    c => c.Endpoint.Equals("http://new"))), Times.Once());
        }
    }
}
