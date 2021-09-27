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

using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    partial class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Show verbose output");
            return await new CommandLineBuilder(new RootCommand($"{Strings.ApplicationName} CLI"))
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureLogging((context, logging) =>
                        {
                            var invocationContext = context.GetInvocationContext();
                            var verboseEnabled = invocationContext.ParseResult.ValueForOption(verboseOption);
                            logging.ClearProviders();

                            logging.AddInformaticsGatewayConsole(options => options.MinimumLogLevel = verboseEnabled ? LogLevel.Trace : LogLevel.Information)
                                .AddFilter("Microsoft", LogLevel.None)
                                .AddFilter("System", LogLevel.None)
                                .AddFilter("*", LogLevel.Trace);
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddScoped<IFileSystem, FileSystem>();
                            services.AddScoped<IConfirmationPrompt, ConfirmationPrompt>();
                            services.AddScoped<IConsoleRegion, ConsoleRegion>();
                            services.AddHttpClient<InformaticsGatewayClient>();
                            services.AddSingleton<IInformaticsGatewayClient>(p => p.GetRequiredService<InformaticsGatewayClient>());
                            services.AddSingleton<IConfigurationService, ConfigurationService>();
                            services.AddSingleton<IControlService, ControlService>();
                            services.AddSingleton<IContainerRunnerFactory, ContainerRunnerFactory>();
                            services.AddSingleton<IEmbeddedResource, EmbeddedResource>();
                            services.AddTransient<DockerRunner>();
                            services.AddTransient<IDockerClient>(p => new DockerClientConfiguration().CreateClient());
                        });
                    })
                .AddGlobalOption(verboseOption)
                .AddCommand(new ConfigCommand())
                .AddCommand(new StartCommand())
                .AddCommand(new StopCommand())
                .AddCommand(new RestartCommand())
                .AddCommand(new AetCommand())
                .AddCommand(new SourceCommand())
                .AddCommand(new DestinationCommand())
                .AddCommand(new StatusCommand())
                .UseAnsiTerminalWhenAvailable()
                .UseExceptionHandler((exception, context) =>
                {
                    Console.Out.WriteLineAsync(Crayon.Output.Bright.Red($"Exception: {exception.Message}"));
                })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }
    }
}
