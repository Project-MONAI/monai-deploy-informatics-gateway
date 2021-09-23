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
using Monai.Deploy.InformaticsGateway.CLI.Services;
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
    public class DestinationCommand : CommandBase
    {
        public DestinationCommand() : base("dst", "Configure DICOM destinations")
        {
            this.AddAlias("dest");
            this.AddAlias("destination");

            SetupAddDestinationCommand();
            SetupRemoveDestinationCommand();
            SetupListDestinationCommand();
        }

        private void SetupListDestinationCommand()
        {
            var listCommand = new Command("ls", "List all DICOM destinations");
            listCommand.AddAlias("list");
            this.AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<DestinationApplicationEntity, IHost, bool, CancellationToken>(ListDestinationHandlerAsync);
        }

        private void SetupRemoveDestinationCommand()
        {
            var removeCommand = new Command("rm", "Remove a DICOM destination");
            removeCommand.AddAlias("del");
            this.AddCommand(removeCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the DICOM destination") { IsRequired = true };
            removeCommand.AddOption(nameOption);

            removeCommand.Handler = CommandHandler.Create<string, IHost, bool, CancellationToken>(RemoveDestinationHandlerAsync);
        }

        private void SetupAddDestinationCommand()
        {
            var addCommand = new Command("add", "Add a new DICOM destination");
            this.AddCommand(addCommand);

            var nameOption = new Option<string>(new string[] { "--name", "-n" }, "Name of the DICOM destination") { IsRequired = false };
            addCommand.AddOption(nameOption);
            var aeTitleOption = new Option<string>(new string[] { "--aetitle", "-a" }, "AE Title of the DICOM destination") { IsRequired = true };
            addCommand.AddOption(aeTitleOption);
            var hostOption = new Option<string>(new string[] { "--host-ip", "-h" }, "Host or IP address of the DICOM destination") { IsRequired = true };
            addCommand.AddOption(hostOption);
            var portOption = new Option<int>(new string[] { "--port", "-p" }, "Listening port of the DICOM destination") { IsRequired = true };
            addCommand.AddOption(portOption);

            addCommand.Handler = CommandHandler.Create<DestinationApplicationEntity, IHost, bool, CancellationToken>(AddDestinationHandlerAsync);
        }

        private async Task<int> ListDestinationHandlerAsync(DestinationApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");

            var console = host.Services.GetRequiredService<IConsole>();
            var config = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var consoleRegion = host.Services.GetRequiredService<IConsoleRegion>();
            var logger = CreateLogger<DestinationCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(console, nameof(console), "Console service is unavailable.");
            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");
            Guard.Against.Null(consoleRegion, nameof(consoleRegion), "Console region is unavailable.");

            IReadOnlyList<DestinationApplicationEntity> items = null;
            try
            {
                client.ConfigureServiceUris(config.InformaticsGatewayServerUri);
                this.LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {config.InformaticsGatewayServer}...");
                this.LogVerbose(verbose, host, $"Retrieving DICOM destinations...");
                items = await client.DicomDestinations.List(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error retrieving DICOM destinations: {ex.Message}");
                return ExitCodes.DestinationAe_ErrorList;
            }

            if (items.IsNullOrEmpty())
            {
                logger.Log(LogLevel.Warning, "No DICOM destinations configured.");
            }
            else
            {
                if (console is ITerminal terminal)
                {
                    terminal.Clear();
                }
                var consoleRenderer = new ConsoleRenderer(console);

                var table = new TableView<DestinationApplicationEntity>
                {
                    Items = items.OrderBy(p => p.Name).ToList()
                };
                table.AddColumn(p => p.Name, new ContentView("Name".Underline()));
                table.AddColumn(p => p.AeTitle, new ContentView("AE Title".Underline()));
                table.AddColumn(p => p.HostIp, new ContentView("Host/IP Address".Underline()));
                table.AddColumn(p => p.Port, new ContentView("Port".Underline()));
                table.Render(consoleRenderer, consoleRegion.GetDefaultConsoleRegion());
            }
            return ExitCodes.Success;
        }

        private async Task<int> RemoveDestinationHandlerAsync(string name, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var config = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<DestinationCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                client.ConfigureServiceUris(config.InformaticsGatewayServerUri);
                this.LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {config.InformaticsGatewayServer}...");
                this.LogVerbose(verbose, host, $"Deleting DICOM destination {name}...");
                _ = await client.DicomDestinations.Delete(name, cancellationToken);
                logger.Log(LogLevel.Information, $"DICOM destination '{name}' deleted.");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error deleting DICOM destination {name}: {ex.Message}");
                return ExitCodes.DestinationAe_ErrorDelete;
            }
            return ExitCodes.Success;
        }

        private async Task<int> AddDestinationHandlerAsync(DestinationApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var config = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<DestinationCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                client.ConfigureServiceUris(config.InformaticsGatewayServerUri);

                this.LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {config.InformaticsGatewayServer}...");
                var result = await client.DicomDestinations.Create(entity, cancellationToken);

                logger.Log(LogLevel.Information, "New DICOM destination created:");
                logger.Log(LogLevel.Information, "\tName:            {0}", result.Name);
                logger.Log(LogLevel.Information, "\tAE Title:        {0}", result.AeTitle);
                logger.Log(LogLevel.Information, "\tHost/IP Address: {0}", result.HostIp);
                logger.Log(LogLevel.Information, "\tPort:            {0}", result.Port);
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error creating DICOM destination {entity.AeTitle}: {ex.Message}");
                return ExitCodes.DestinationAe_ErrorCreate;
            }
            return ExitCodes.Success;
        }
    }
}
