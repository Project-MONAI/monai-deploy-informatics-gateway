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
            SetupEditDestinationCommand();
            SetupRemoveDestinationCommand();
            SetupListDestinationCommand();
            SetupCEchoCommand();
            SetupPlugInsCommand();
        }

        private void SetupCEchoCommand()
        {
            var cechoCommand = new Command("cecho", "C-ECHO a DICOM destination");
            cechoCommand.AddAlias("cecho");
            AddCommand(cechoCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the DICOM destination") { IsRequired = true };
            cechoCommand.AddOption(nameOption);

            cechoCommand.Handler = CommandHandler.Create<string, IHost, bool, CancellationToken>(CEchoDestinationHandlerAsync);
        }

        private void SetupListDestinationCommand()
        {
            var listCommand = new Command("ls", "List all DICOM destinations");
            listCommand.AddAlias("list");
            AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<DestinationApplicationEntity, IHost, bool, CancellationToken>(ListDestinationHandlerAsync);
        }

        private void SetupEditDestinationCommand()
        {
            var addCommand = new Command("update", "Update a new DICOM destination");
            AddCommand(addCommand);

            var nameOption = new Option<string>(new string[] { "--name", "-n" }, "Name of the DICOM destination") { IsRequired = false };
            addCommand.AddOption(nameOption);
            var aeTitleOption = new Option<string>(new string[] { "--aetitle", "-a" }, "AE Title of the DICOM destination") { IsRequired = true };
            addCommand.AddOption(aeTitleOption);
            var hostOption = new Option<string>(new string[] { "--host-ip", "-h" }, "Host or IP address of the DICOM destination") { IsRequired = true };
            addCommand.AddOption(hostOption);
            var portOption = new Option<int>(new string[] { "--port", "-p" }, "Listening port of the DICOM destination") { IsRequired = true };
            addCommand.AddOption(portOption);

            addCommand.Handler = CommandHandler.Create<DestinationApplicationEntity, IHost, bool, CancellationToken>(EditDestinationHandlerAsync);
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

        private void SetupPlugInsCommand()
        {
            var pluginsCommand = new Command("plugins", "List all available plug-ins for DICOM destinations");
            AddCommand(pluginsCommand);

            pluginsCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ListPlugInsHandlerAsync);
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

                logger.ListedNItems(items.Count);
            }
            return ExitCodes.Success;
        }

        private async Task<int> CEchoDestinationHandlerAsync(string name, IHost host, bool verbose, CancellationToken cancellationToken)
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
                await client.DicomDestinations.CEcho(name, cancellationToken).ConfigureAwait(false);
                logger.DicomCEchoSuccessful(name);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorCEchogDicomDestination(name, ex.Message);
                return ExitCodes.DestinationAe_ErrorCEcho;
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

        private async Task<int> EditDestinationHandlerAsync(DestinationApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
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
                LogVerbose(verbose, host, $"Updating DICOM destination {entity.AeTitle}...");
                var result = await client.DicomDestinations.Update(entity, cancellationToken).ConfigureAwait(false);

                logger.DicomDestinationCreated(result.Name, result.AeTitle, result.HostIp, result.Port);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorUpdatingDicomDestination(entity.AeTitle, ex.Message);
                return ExitCodes.DestinationAe_ErrorUpdate;
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

        private async Task<int> ListPlugInsHandlerAsync(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");

            var console = host.Services.GetRequiredService<IConsole>();
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var consoleRegion = host.Services.GetRequiredService<IConsoleRegion>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(console, nameof(console), "Console service is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");
            Guard.Against.Null(consoleRegion, nameof(consoleRegion), "Console region is unavailable.");

            IDictionary<string, string> items = null;
            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Retrieving MONAI SCP AE Titles...");
                items = await client.DicomDestinations.PlugIns(cancellationToken).ConfigureAwait(false);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorListingDataOutputPlugIns(ex.Message);
                return ExitCodes.DestinationAe_ErrorPlugIns;
            }

            if (items.IsNullOrEmpty())
            {
                logger.NoAeTitlesFound();
            }
            else
            {
                if (console is ITerminal terminal)
                {
                    terminal.Clear();
                }
                var consoleRenderer = new ConsoleRenderer(console);

                var table = new TableView<KeyValuePair<string, string>>
                {
                    Items = items.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)).ToList()
                };
                table.AddColumn(p => p.Key, new ContentView("Name".Underline()));
                table.AddColumn(p => p.Value, new ContentView("Assembly Name".Underline()));
                table.Render(consoleRenderer, consoleRegion.GetDefaultConsoleRegion());

                logger.ListedNItems(items.Count);
            }
            return ExitCodes.Success;
        }
    }
}
