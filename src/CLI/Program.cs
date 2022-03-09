// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    internal partial class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Show verbose output");
            return await new CommandLineBuilder(new RootCommand($"{Strings.ApplicationName} CLI"))
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        _ = host.ConfigureLogging((context, logging) =>
                        {
                            var invocationContext = context.GetInvocationContext();
                            var verboseEnabled = invocationContext.ParseResult.ValueForOption(verboseOption);
                            logging.ClearProviders();

                            _ = logging.AddInformaticsGatewayConsole(options => options.MinimumLogLevel = verboseEnabled ? LogLevel.Trace : LogLevel.Information)
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
