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

using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class SourceCommand : CommandBase
    {
        public SourceCommand() : base("src", "Configure DICOM sources")
        {
            this.AddAlias("source");

            SetupAddSourceCommand();
            SetupRemoveSourceCommand();
            SetupListSourceCommand();
        }

        private void SetupListSourceCommand()
        {
            var listCommand = new Command("ls", "List all DICOM sources");
            listCommand.AddAlias("list");
            this.AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<SourceApplicationEntity, IHost, bool, CancellationToken>(ListSourceHandlerAsync);
        }

        private void SetupRemoveSourceCommand()
        {
            var removeCommand = new Command("rm", "Remove a DICOM source");
            removeCommand.AddAlias("del");
            this.AddCommand(removeCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the DICOM source") { IsRequired = true };
            removeCommand.AddOption(nameOption);

            removeCommand.Handler = CommandHandler.Create<string, IHost, bool, CancellationToken>(RemoveSourceHandlerAsync);
        }

        private void SetupAddSourceCommand()
        {
            var addCommand = new Command("add", "Add a new DICOM source");
            this.AddCommand(addCommand);

            var nameOption = new Option<string>(new string[] { "--name", "-n" }, "Name of the DICOM source") { IsRequired = false };
            addCommand.AddOption(nameOption);
            var aeTitleOption = new Option<string>(new string[] { "--aetitle", "-a" }, "AE Title of the DICOM source") { IsRequired = true };
            addCommand.AddOption(aeTitleOption);
            var hostOption = new Option<string>(new string[] { "--host-ip", "-h" }, "Host or IP address of the DICOM source") { IsRequired = true };
            addCommand.AddOption(hostOption);

            addCommand.Handler = CommandHandler.Create<SourceApplicationEntity, IHost, bool, CancellationToken>(AddSourceHandlerAsync);
        }

        private async Task ListSourceHandlerAsync(SourceApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationTokena)
        {
            this.LogVerbose(verbose, host, "Configuring services...");

            var console = host.Services.GetRequiredService<IConsole>();
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<InformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(console, nameof(console), "Console service is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), "Informatics Gateway client is unavailable.");

            var config = configService.Load(verbose);
            client.ConfigureServiceUris(new Uri(config.Endpoint));

            this.LogVerbose(verbose, host, $"Connecting to Informatics Gateway at {config.Endpoint}...");
            this.LogVerbose(verbose, host, $"Retrieving DICOM sources...");

            IReadOnlyList<SourceApplicationEntity> items = null;
            try
            {
                items = await client.DicomSources.List(cancellationTokena);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error retrieving DICOM sources: {ex.Message}");
                return;
            }

            if (items.IsNullOrEmpty())
            {
                logger.Log(LogLevel.Warning, "No DICOM sources configured.");
            }
            else
            {
                if (console is ITerminal terminal)
                {
                    terminal.Clear();
                }
                var consoleRenderer = new ConsoleRenderer(console);

                var table = new TableView<SourceApplicationEntity>
                {
                    Items = items.OrderBy(p => p.Name).ToList()
                };
                table.AddColumn(p => p.Name, new ContentView("Name".Underline()));
                table.AddColumn(p => p.AeTitle, new ContentView("AE Title".Underline()));
                table.AddColumn(p => p.HostIp, new ContentView("Host/IP Address".Underline()));
                table.Render(consoleRenderer, new Region(0, 0, Console.WindowWidth, Console.WindowHeight, false));
            }
        }

        private async Task RemoveSourceHandlerAsync(string name, IHost host, bool verbose, CancellationToken cancellationTokena)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<InformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), "Informatics Gateway client is unavailable.");

            var config = configService.Load(verbose);
            client.ConfigureServiceUris(new Uri(config.Endpoint));

            this.LogVerbose(verbose, host, $"Connecting to Informatics Gateway at {config.Endpoint}...");
            this.LogVerbose(verbose, host, $"Deleting DICOM source {name}...");
            try
            {
                _ = await client.DicomSources.Delete(name, cancellationTokena);
                logger.Log(LogLevel.Information, $"DICOM source '{name}' deleted.");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error deleting DICOM source {name}: {ex.Message}");
            }
        }

        private async Task AddSourceHandlerAsync(SourceApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationTokena)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<InformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), "Informatics Gateway client is unavailable.");

            var config = configService.Load(verbose);
            client.ConfigureServiceUris(new Uri(config.Endpoint));

            this.LogVerbose(verbose, host, $"Connecting to Informatics Gateway at {config.Endpoint}...");
            this.LogVerbose(verbose, host, $"Creating new DICOM source {entity.AeTitle}...");

            try
            {
                var result = await client.DicomSources.Create(entity, cancellationTokena);

                logger.Log(LogLevel.Information, "New DICOM source created:");
                logger.Log(LogLevel.Information, "\tName:            {0}", result.Name);
                logger.Log(LogLevel.Information, "\tAE Title:        {0}", result.AeTitle);
                logger.Log(LogLevel.Information, "\tHOST/IP Address: {0}", result.HostIp);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error creating DICOM source {entity.AeTitle}: {ex.Message}");
            }
        }
    }
}