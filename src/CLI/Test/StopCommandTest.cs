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
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Moq;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class StopCommandTest

    {
        private readonly Mock<IControlService> _controlService;
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<IConfirmationPrompt> _confirmationPrompt;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;

        public StopCommandTest()
        {
            _confirmationPrompt = new Mock<IConfirmationPrompt>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger>();
            _controlService = new Mock<IControlService>();
            _commandLineBuilder = new CommandLineBuilder()
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureServices(services =>
                        {
                            services.AddSingleton<IConfirmationPrompt>(p => _confirmationPrompt.Object);
                            services.AddSingleton<ILoggerFactory>(p => _loggerFactory.Object);
                            services.AddSingleton<IControlService>(p => _controlService.Object);
                        });
                    })
                .AddCommand(new StopCommand());
            _paser = _commandLineBuilder.Build();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
        }

        [Fact(DisplayName = "stop comand - cancelled")]
        public async Task StopCommand_Cancelled()
        {
            var command = "stop";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _confirmationPrompt.Setup(p => p.ShowConfirmationPrompt(It.IsAny<string>())).Returns(false);
            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Stop_Cancelled, exitCode);

            _controlService.Verify(p => p.Stop(It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact(DisplayName = "stop comand - confirmed")]
        public async Task StopCommand_Confirmed()
        {
            var command = "stop";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _confirmationPrompt.Setup(p => p.ShowConfirmationPrompt(It.IsAny<string>())).Returns(true);
            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);

            _controlService.Verify(p => p.Stop(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "stop comand -y")]
        public async Task StopCommand_Auto()
        {
            var command = "stop -y";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);

            _controlService.Verify(p => p.Stop(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "stop comand -y excception")]
        public async Task StopCommand_Auto_Exception()
        {
            var command = "stop -y";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _controlService.Setup(p => p.Stop(It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Stop_Error, exitCode);

            _controlService.Verify(p => p.Stop(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
