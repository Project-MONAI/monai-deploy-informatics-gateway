// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
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
        private readonly Mock<IConfirmationPrompt> _confirmationPrompt;

        public ConfigCommandTest()
        {
            _confirmationPrompt = new Mock<IConfirmationPrompt>();
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
                            services.AddSingleton<IConfirmationPrompt>(p => _confirmationPrompt.Object);
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
            Assert.Equal("Required command was not provided.", result.Errors.First().Message);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);
        }

        [Fact(DisplayName = "config show comand when not yet configured")]
        public async Task ConfigShow_Command_NotConfigured()
        {
            var command = "config show";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);
        }

        [Fact(DisplayName = "config show comand ")]
        public async Task ConfigShow_Command()
        {
            var command = "config show";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerEndpoint).Returns("http://test");
            _configurationService.SetupGet(p => p.Configurations.DicomListeningPort).Returns(100);
            _configurationService.SetupGet(p => p.Configurations.Runner).Returns(Runner.Docker);
            _configurationService.SetupGet(p => p.Configurations.HostDatabaseStorageMount).Returns("DB");
            _configurationService.SetupGet(p => p.Configurations.HostDataStorageMount).Returns("Data");
            _configurationService.SetupGet(p => p.Configurations.HostLogsStorageMount).Returns("Logs");

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _logger.VerifyLogging("Informatics Gateway API: http://test", LogLevel.Information, Times.Once());
            _logger.VerifyLogging("DICOM SCP Listening Port: 100", LogLevel.Information, Times.Once());
            _logger.VerifyLogging("Container Runner: Docker", LogLevel.Information, Times.Once());
            _logger.VerifyLogging("Host:", LogLevel.Information, Times.Once());
            _logger.VerifyLogging("   Database storage mount: DB", LogLevel.Information, Times.Once());
            _logger.VerifyLogging("   Data storage mount: Data", LogLevel.Information, Times.Once());
            _logger.VerifyLogging("   Logs storage mount: Logs", LogLevel.Information, Times.Once());
        }

        [Fact(DisplayName = "config show comand exception")]
        public async Task ConfigShow_Command_Exception()
        {
            var command = "config show";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerEndpoint).Throws(new System.Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_ErrorShowing, exitCode);
        }

        [Fact(DisplayName = "config runner command")]
        public async Task ConfigRunner_Command()
        {
            var command = "config runner helm";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            var callbackResult = Runner.Docker;
            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupSet<Runner>(p => p.Configurations.Runner = It.IsAny<Runner>())
                .Callback(value => callbackResult = value);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Equal(Runner.Helm, callbackResult);
        }

        [Fact(DisplayName = "config endpoint command")]
        public async Task ConfigEndpoint_Command()
        {
            var command = "config endpoint http://test:123";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            var callbackResult = string.Empty;
            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupSet<string>(p => p.Configurations.InformaticsGatewayServerEndpoint = It.IsAny<string>())
                .Callback(value => callbackResult = value);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);
            Assert.Equal("http://test:123", callbackResult);
        }

        [Fact(DisplayName = "config endpoint command exception")]
        public async Task ConfigEndpoint_Command_Exception()
        {
            var command = "config endpoint http://test:123";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            var callbackResult = string.Empty;
            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupSet<string>(p => p.Configurations.InformaticsGatewayServerEndpoint = It.IsAny<string>())
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_ErrorSaving, exitCode);
        }

        [Fact(DisplayName = "config endpoint command config exception")]
        public async Task ConfigEndpoint_Command_CopnfigException()
        {
            var command = "config endpoint http://test:123";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            var callbackResult = string.Empty;
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);
        }

        [Fact(DisplayName = "config init command")]
        public async Task ConfigInit_Command()
        {
            var command = "config init";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.SetupGet(p => p.IsConfigExists).Returns(false);
            _configurationService.Setup(p => p.Initialize(It.IsAny<CancellationToken>()));

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);
            _configurationService.Verify(p => p.Initialize(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "config init command bypass prompt")]
        public async Task ConfigInit_Command_BypassPrompt()
        {
            var command = "config init -y";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.Setup(p => p.Initialize(It.IsAny<CancellationToken>()));

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);
            _configurationService.Verify(p => p.Initialize(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "config init command exception")]
        public async Task ConfigInit_Command_Exception()
        {
            var command = "config init -y";

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            _configurationService.Setup(p => p.Initialize(It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Config_ErrorInitializing, exitCode);
            _configurationService.Verify(p => p.Initialize(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "config init command cancelled")]
        public async Task ConfigInit_Command_Cancelled()
        {
            var command = "config init";
            _confirmationPrompt.Setup(p => p.ShowConfirmationPrompt(It.IsAny<string>())).Returns(false);
            _configurationService.SetupGet(p => p.IsConfigExists).Returns(true);

            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Stop_Cancelled, exitCode);
        }
    }
}
