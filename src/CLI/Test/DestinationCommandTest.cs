/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Globalization;
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
    public class DestinationCommandTest
    {
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IInformaticsGatewayClient> _informaticsGatewayClient;

        public DestinationCommandTest()
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
            _commandLineBuilder.Command.AddCommand(new DestinationCommand());
            _paser = _commandLineBuilder.Build();

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupGet(p => p.IsConfigExists).Returns(true);
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerUri).Returns(new Uri("http://test"));
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerEndpoint).Returns("http://test");
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "dst command")]
        public async Task Dst_Command()
        {
            var command = "dst";
            var result = _paser.Parse(command);
            Assert.Equal("Required command was not provided.", result.Errors.First().Message);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);
        }

        [Fact(DisplayName = "dst add command")]
        public async Task DstAdd_Command()
        {
            var command = "dst add -n MyName -a MyAET -h MyHost -p 100";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new DestinationApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = result.CommandResult.Children[1].Tokens[0].Value,
                HostIp = result.CommandResult.Children[2].Tokens[0].Value,
                Port = int.Parse(result.CommandResult.Children[3].Tokens[0].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture),
            };

            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Equal("MyHost", entity.HostIp);
            Assert.Equal(100, entity.Port);

            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Create(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.DicomDestinations.Create(
                    It.Is<DestinationApplicationEntity>(o => o.AeTitle == entity.AeTitle && o.Name == entity.Name && o.HostIp == entity.HostIp && o.Port == entity.Port),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "dst add command exception")]
        public async Task DstAdd_Command_Exception()
        {
            var command = "dst add -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Create(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.DestinationAe_ErrorCreate, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.Create(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error creating DICOM destination", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "dst add command configuration exception")]
        public async Task DstAdd_Command_ConfigurationException()
        {
            var command = "dst add -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `", LogLevel.Critical, Times.Once());
            _logger.VerifyLoggingMessageEndsWith(" config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());

        }

        [Fact(DisplayName = "dst remove command")]
        public async Task DstRemove_Command()
        {
            var command = "dst rm -n MyName";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var name = result.CommandResult.Children[0].Tokens[0].Value;
            Assert.Equal("MyName", name);

            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.Delete(It.Is<string>(o => o.Equals(name)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "dst remove command exception")]
        public async Task DstRemove_Command_Exception()
        {
            var command = "dst rm -n MyName";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.DestinationAe_ErrorDelete, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error deleting DICOM destination", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "dst remove command configuration exception")]
        public async Task DstRemove_Command_ConfigurationException()
        {
            var command = "dst rm -n MyName";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `", LogLevel.Critical, Times.Once());
            _logger.VerifyLoggingMessageEndsWith(" config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());

        }

        [Fact(DisplayName = "dst list command")]
        public async Task DstList_Command()
        {
            var command = "dst list";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new DestinationApplicationEntity()
            {
                Name = "MyName",
                AeTitle = "MyAET",
                HostIp = "MyHost",
                Port = 100
            };

            _informaticsGatewayClient.Setup(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DestinationApplicationEntity>() { entity });

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "dst list command exception")]
        public async Task DstList_Command_Exception()
        {
            var command = "dst list";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.DestinationAe_ErrorList, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error retrieving DICOM destinations", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "dst list command configuration exception")]
        public async Task DstList_Command_ConfigurationException()
        {
            var command = "dst list";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `", LogLevel.Critical, Times.Once());
            _logger.VerifyLoggingMessageEndsWith(" config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());

        }

        [Fact(DisplayName = "dst list command empty")]
        public async Task DstList_Command_Empty()
        {
            var command = "dst list";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DestinationApplicationEntity>());

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging("No DICOM destinations configured.", LogLevel.Warning, Times.Once());
        }

        [Fact(DisplayName = "dst cecho command")]
        public async Task DstCEcho_Command()
        {
            var command = "dst cecho -n MyName";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var name = result.CommandResult.Children[0].Tokens[0].Value;
            Assert.Equal("MyName", name);

            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.CEcho(It.Is<string>(o => o.Equals(name)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "dst cecho command exception")]
        public async Task DstCEcho_Command_Exception()
        {
            var command = "dst cecho -n MyName";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.CEcho(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.DestinationAe_ErrorCEcho, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.CEcho(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("C-ECHO to MyName failed", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "dst cecho command configuration exception")]
        public async Task DstCEcho_Command_ConfigurationException()
        {
            var command = "dst cecho -n MyName";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.CEcho(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `", LogLevel.Critical, Times.Once());
            _logger.VerifyLoggingMessageEndsWith(" config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());

        }

        [Fact(DisplayName = "dst update command")]
        public async Task DstUpdate_Command()
        {
            var command = "dst update -n MyName -a MyAET -h MyHost -p 100";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new DestinationApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = result.CommandResult.Children[1].Tokens[0].Value,
                HostIp = result.CommandResult.Children[2].Tokens[0].Value,
                Port = int.Parse(result.CommandResult.Children[3].Tokens[0].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture),
            };

            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Equal("MyHost", entity.HostIp);
            Assert.Equal(100, entity.Port);

            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Update(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.DicomDestinations.Update(
                    It.Is<DestinationApplicationEntity>(o => o.AeTitle == entity.AeTitle && o.Name == entity.Name && o.HostIp == entity.HostIp && o.Port == entity.Port),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "dst update command exception")]
        public async Task DstUpdate_Command_Exception()
        {
            var command = "dst update -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.Update(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.DestinationAe_ErrorUpdate, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.Update(It.IsAny<DestinationApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error updating DICOM destination", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "dst update command configuration exception")]
        public async Task DstUpdate_Command_ConfigurationException()
        {
            var command = "dst update -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `", LogLevel.Critical, Times.Once());
            _logger.VerifyLoggingMessageEndsWith(" config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());

        }

        [Fact(DisplayName = "dst plugins comand")]
        public async Task DstPlugIns_Command()
        {
            var command = "dst plugins";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entries = new Dictionary<string, string> { { "A", "1" }, { "B", "2" } };

            _informaticsGatewayClient.Setup(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "dst plugins comand exception")]
        public async Task DstPlugIns_Command_Exception()
        {
            var command = "dst plugins";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.DestinationAe_ErrorPlugIns, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error retrieving data output plug-ins", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "dst plugins comand configuration exception")]
        public async Task DstPlugIns_Command_ConfigurationException()
        {
            var command = "dst plugins";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `", LogLevel.Critical, Times.Once());
            _logger.VerifyLoggingMessageEndsWith(" config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());

        }

        [Fact(DisplayName = "dst plugins comand empty")]
        public async Task DstPlugIns_Command_Empty()
        {
            var command = "dst plugins";
            _informaticsGatewayClient.Setup(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, string>());

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.PlugIns(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging("No MONAI SCP Application Entities configured.", LogLevel.Warning, Times.Once());
        }
    }
}
