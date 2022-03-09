// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class RestartCommandTest

    {
        private readonly Mock<IControlService> _controlService;
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<IConfirmationPrompt> _confirmationPrompt;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;

        public RestartCommandTest()
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
                .AddCommand(new RestartCommand());
            _paser = _commandLineBuilder.Build();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
        }

        [Fact(DisplayName = "restart comand - cancelled")]
        public async Task RestartCommand_Cancelled()
        {
            var command = "restart";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _confirmationPrompt.Setup(p => p.ShowConfirmationPrompt(It.IsAny<string>())).Returns(false);
            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Restart_Cancelled, exitCode);

            _controlService.Verify(p => p.RestartService(It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact(DisplayName = "restart comand - confirmed")]
        public async Task RestartCommand_Confirmed()
        {
            var command = "restart";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _confirmationPrompt.Setup(p => p.ShowConfirmationPrompt(It.IsAny<string>())).Returns(true);
            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);

            _controlService.Verify(p => p.RestartService(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "restart comand -y")]
        public async Task RestartCommand_Auto()
        {
            var command = "restart -y";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);

            _controlService.Verify(p => p.RestartService(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "restart comand -y excception")]
        public async Task RestartCommand_Auto_Exception()
        {
            var command = "restart -y";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _controlService.Setup(p => p.RestartService(It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Restart_Error, exitCode);

            _controlService.Verify(p => p.RestartService(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
