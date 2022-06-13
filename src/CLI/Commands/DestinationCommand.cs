// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Common;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class DestinationCommand : CommandBase
    {
        public DestinationCommand() : base("dst", "Configure DICOM destinations")
        {
            AddAlias("dest");
            AddAlias("destination");

            SetupAddDestinationCommand();
            SetupRemoveDestinationCommand();
            SetupListDestinationCommand();
        }

        private void SetupListDestinationCommand()
        {
            var listCommand = new Command("ls", "List all DICOM destinations");
            listCommand.AddAlias("list");
            AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<DestinationApplicationEntity, IHost, bool, CancellationToken>(ListDestinationHandlerAsync);
        }

        private void SetupRemoveDestinationCommand()
        {
            var removeCommand = new Command("rm", "Remove a DICOM destination");
            removeCommand.AddAlias("del");
            AddCommand(removeCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the DICOM destination") { IsRequired = true };
            removeCommand.AddOption(nameOption);

            removeCommand.Handler = CommandHandler.Create<string, IHost, bool, CancellationToken>(RemoveDestinationHandlerAsync);
        }

        private void SetupAddDestinationCommand()
        {
            var addCommand = new Command("add", "Add a new DICOM destination");
            AddCommand(addCommand);

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
            Guard.Against.Null(entity, nameof(entity));
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");

            var console = host.Services.GetRequiredService<IConsole>();
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var consoleRegion = host.Services.GetRequiredService<IConsoleRegion>();
            var logger = CreateLogger<DestinationCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(console, nameof(console), "Console service is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");
            Guard.Against.Null(consoleRegion, nameof(consoleRegion), "Console region is unavailable.");

            IReadOnlyList<DestinationApplicationEntity> items = null;
            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Retrieving DICOM destinations...");
                items = await client.DicomDestinations.List(cancellationToken).ConfigureAwait(false);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorListingDicomDestinations(ex.Message);
                return ExitCodes.DestinationAe_ErrorList;
            }

            if (items.IsNullOrEmpty())
            {
                logger.NoDicomDestinationFound();
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
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<DestinationCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Deleting DICOM destination {name}...");
                _ = await client.DicomDestinations.Delete(name, cancellationToken).ConfigureAwait(false);
                logger.DicomDestinationDeleted(name);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorDeletingDicomDestination(name, ex.Message);
                return ExitCodes.DestinationAe_ErrorDelete;
            }
            return ExitCodes.Success;
        }

        private async Task<int> AddDestinationHandlerAsync(DestinationApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(entity, nameof(entity));
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<DestinationCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);

                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                var result = await client.DicomDestinations.Create(entity, cancellationToken).ConfigureAwait(false);

                logger.DicomDestinationCreated(result.Name, result.AeTitle, result.HostIp, result.Port);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorCreatingDicomDestination(entity.AeTitle, ex.Message);
                return ExitCodes.DestinationAe_ErrorCreate;
            }
            return ExitCodes.Success;
        }
    }
}
