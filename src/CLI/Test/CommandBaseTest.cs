// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class CommandBaseTest
    {
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;

        public CommandBaseTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger>();
            _commandLineBuilder = new CommandLineBuilder()
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureServices(services =>
                        {
                            services.AddSingleton<ILoggerFactory>(p => _loggerFactory.Object);
                        });
                    });
            _commandLineBuilder.Command.AddGlobalOption(new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Show verbose output"));
            _commandLineBuilder.Command.AddCommand(new TestCommand());
            _paser = _commandLineBuilder.Build();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "LogVerbose Verbose with logger")]
        public async Task LogVerbose_VerboseWithLogger()
        {
            var command = "test -v";
            var result = _paser.Parse(command);
            Assert.Equal(0, result.Errors.Count);

            var exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(0, exitCode);

            _logger.VerifyLogging("this is a test", LogLevel.Debug, Times.Once());
        }
    }

    internal class TestCommand : CommandBase
    {
        public TestCommand() : base("test", "description")
        {
            Handler = CommandHandler.Create<IHost, bool>(TestHandler);
        }

        private void TestHandler(IHost host, bool verbose)
        {
            LogVerbose(verbose, host, "this is a test");
        }
    }
}
