// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class SourceCommandTest
    {
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IInformaticsGatewayClient> _informaticsGatewayClient;

        public SourceCommandTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger>();
            _configurationService = new Mock<IConfigurationService>();
            _informaticsGatewayClient = new Mock<IInformaticsGatewayClient>();
            _commandLineBuilder = new CommandLineBuilder()
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureServices(services =>
                        {
                            services.AddScoped<IConsoleRegion, TestConsoleRegion>();
                            services.AddSingleton<IConsole, TestConsole>();
                            services.AddSingleton<ITerminal, TestTerminal>();
                            services.AddSingleton<ILoggerFactory>(p => _loggerFactory.Object);
                            services.AddSingleton<IInformaticsGatewayClient>(p => _informaticsGatewayClient.Object);
                            services.AddSingleton<IConfigurationService>(p => _configurationService.Object);
                        });
                    });
            _commandLineBuilder.Command.AddCommand(new SourceCommand());
            _paser = _commandLineBuilder.Build();

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupGet(p => p.IsConfigExists).Returns(true);
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerUri).Returns(new Uri("http://test"));
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerEndpoint).Returns("http://test");
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "src comand")]
        public async Task Src_Command()
        {
            var command = "src";
            var result = _paser.Parse(command);
            Assert.Equal("Required command was not provided.", result.Errors.First().Message);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);
        }

        [Fact(DisplayName = "src add comand")]
        public async Task SrcAdd_Command()
        {
            var command = "src add -n MyName -a MyAET -h MyHost";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new SourceApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = result.CommandResult.Children[1].Tokens[0].Value,
                HostIp = result.CommandResult.Children[2].Tokens[0].Value,
            };

            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Equal("MyHost", entity.HostIp);

            _informaticsGatewayClient.Setup(p => p.DicomSources.Create(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.DicomSources.Create(
                    It.Is<SourceApplicationEntity>(o => o.AeTitle == entity.AeTitle && o.Name == entity.Name && o.HostIp == entity.HostIp),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "src add comand exception")]
        public async Task SrcAdd_Command_Exception()
        {
            var command = "src add -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _informaticsGatewayClient.Setup(p => p.DicomSources.Create(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.SourceAe_ErrorCreate, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomSources.Create(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error creating DICOM source", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "src add comand configuration exception")]
        public async Task SrcAdd_Command_ConfigurationException()
        {
            var command = "src add -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "src remove comand")]
        public async Task SrcRemove_Command()
        {
            var command = "src rm -n MyName";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var name = result.CommandResult.Children[0].Tokens[0].Value;
            Assert.Equal("MyName", name);

            _informaticsGatewayClient.Setup(p => p.DicomSources.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomSources.Delete(It.Is<string>(o => o.Equals(name)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "src remove comand exception")]
        public async Task SrcRemove_Command_Exception()
        {
            var command = "src rm -n MyName";
            _informaticsGatewayClient.Setup(p => p.DicomSources.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.SourceAe_ErrorDelete, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomSources.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error deleting DICOM source", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "src remove comand configuration exception")]
        public async Task SrcRemove_Command_ConfigurationException()
        {
            var command = "src rm -n MyName";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "src list comand")]
        public async Task SrcList_Command()
        {
            var command = "src list";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new SourceApplicationEntity()
            {
                Name = "MyName",
                AeTitle = "MyAET",
                HostIp = "MyHost",
            };

            _informaticsGatewayClient.Setup(p => p.DicomSources.List(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SourceApplicationEntity>() { entity });

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomSources.List(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "src list comand exception")]
        public async Task SrcList_Command_Exception()
        {
            var command = "src list";
            _informaticsGatewayClient.Setup(p => p.DicomSources.List(It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.SourceAe_ErrorList, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomSources.List(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error retrieving DICOM sources", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "src list comand configuration exception")]
        public async Task SrcList_Command_ConfigurationException()
        {
            var command = "src list";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "src list comand empty")]
        public async Task SrcList_Command_Empty()
        {
            var command = "src list";
            _informaticsGatewayClient.Setup(p => p.DicomSources.List(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SourceApplicationEntity>());

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomSources.List(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging("No DICOM sources configured.", LogLevel.Warning, Times.Once());
        }
    }
}
