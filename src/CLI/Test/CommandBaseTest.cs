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
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
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
                    })
                .AddGlobalOption(new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Show verbose output"))
                .AddCommand(new TestCommand());
            _paser = _commandLineBuilder.Build();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
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
