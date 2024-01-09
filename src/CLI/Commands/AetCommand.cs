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
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Common;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class AetCommand : CommandBase
    {
        public AetCommand() : base("aet", "Configure SCP Application Entities")
        {
            AddAlias("aetitle");

            SetupAddAetCommand();
            SetupEditAetCommand();
            SetupRemoveAetCommand();
            SetupListAetCommand();
            SetupPlugInsCommand();
        }

        private void SetupListAetCommand()
        {
            var listCommand = new Command("ls", "List all SCP Application Entities");
            listCommand.AddAlias("list");
            AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ListAeTitlehandlerAsync);
        }

        private void SetupRemoveAetCommand()
        {
            var removeCommand = new Command("rm", "Remove a SCP Application Entity");
            removeCommand.AddAlias("del");
            AddCommand(removeCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the SCP Application Entity") { IsRequired = true };
            removeCommand.AddOption(nameOption);

            removeCommand.Handler = CommandHandler.Create<string, IHost, bool, CancellationToken>(RemoveAeTitlehandlerAsync);
        }

        private void SetupAddAetCommand()
        {
            var addCommand = new Command("add", "Add a new SCP Application Entity");
            AddCommand(addCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the SCP Application Entity") { IsRequired = false };
            addCommand.AddOption(nameOption);
            var aeTitleOption = new Option<string>(new string[] { "-a", "--aetitle" }, "AE Title of the SCP") { IsRequired = true };
            addCommand.AddOption(aeTitleOption);
            var groupingOption = new Option<string>(new string[] { "-g", "--grouping" }, getDefaultValue: () => "0020,000D", "DICOM tag used to group instances") { IsRequired = false };
            addCommand.AddOption(groupingOption);
            var timeoutOption = new Option<uint>(new string[] { "-t", "--timeout" }, getDefaultValue: () => 5, "Timeout, in seconds, to wait for instances") { IsRequired = false };
            addCommand.AddOption(timeoutOption);
            var workflowsOption = new Option<List<string>>(new string[] { "-w", "--workflows" }, description: "A space separated list of workflow names or IDs to be associated with the SCP AE Title")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(workflowsOption);
            var ignoredSopsOption = new Option<List<string>>(new string[] { "-i", "--ignored-sop-classes" }, description: "A space separated list of SOP Class UIDs to be ignored")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(ignoredSopsOption);
            var allowedSopsOption = new Option<List<string>>(new string[] { "-s", "--allowed-sop-classes" }, description: "A space separated list of SOP Class UIDs to be accepted")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(allowedSopsOption);
            var plugins = new Option<List<string>>(new string[] { "-p", "--plugins" }, description: "A space separated list of fully qualified type names of the plug-ins (surround each plug-in with double quotes)")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(plugins);

            addCommand.Handler = CommandHandler.Create<MonaiApplicationEntity, IHost, bool, CancellationToken>(AddAeTitlehandlerAsync);
        }

        private void SetupEditAetCommand()
        {
            var addCommand = new Command("update", "Update a SCP Application Entities");
            AddCommand(addCommand);

            var nameOption = new Option<string>(new string[] { "-n", "--name" }, "Name of the SCP Application Entity") { IsRequired = false };
            addCommand.AddOption(nameOption);
            var groupingOption = new Option<string>(new string[] { "-g", "--grouping" }, getDefaultValue: () => "0020,000D", "DICOM tag used to group instances") { IsRequired = false };
            addCommand.AddOption(groupingOption);
            var timeoutOption = new Option<uint>(new string[] { "-t", "--timeout" }, getDefaultValue: () => 5, "Timeout, in seconds, to wait for instances") { IsRequired = false };
            addCommand.AddOption(timeoutOption);
            var workflowsOption = new Option<List<string>>(new string[] { "-w", "--workflows" }, description: "A space separated list of workflow names or IDs to be associated with the SCP AE Title")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(workflowsOption);
            var ignoredSopsOption = new Option<List<string>>(new string[] { "-i", "--ignored-sop-classes" }, description: "A space separated list of SOP Class UIDs to be ignored")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(ignoredSopsOption);
            var allowedSopsOption = new Option<List<string>>(new string[] { "-s", "--allowed-sop-classes" }, description: "A space separated list of SOP Class UIDs to be accepted")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(allowedSopsOption);
            var plugins = new Option<List<string>>(new string[] { "-p", "--plugins" }, description: "A space separated list of fully qualified type names of the plug-ins (surround each plug-in with double quotes)")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
            };
            addCommand.AddOption(plugins);

            addCommand.Handler = CommandHandler.Create<MonaiApplicationEntity, IHost, bool, CancellationToken>(EditAeTitleHandlerAsync);
        }

        private void SetupPlugInsCommand()
        {
            var pluginsCommand = new Command("plugins", "List all available plug-ins for SCP Application Entities");
            AddCommand(pluginsCommand);

            pluginsCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ListPlugInsHandlerAsync);
        }

        private async Task<int> ListAeTitlehandlerAsync(IHost host, bool verbose, CancellationToken cancellationToken)
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

            IReadOnlyList<MonaiApplicationEntity>? items = null;
            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Retrieving MONAI SCP AE Titles...");
                items = await client.MonaiScpAeTitle.List(cancellationToken).ConfigureAwait(false);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorListingMonaiAeTitles(ex.Message);
                return ExitCodes.MonaiScp_ErrorList;
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

                var table = new TableView<MonaiApplicationEntity>
                {
                    Items = items.OrderBy(p => p.Name).ToList()
                };
                table.AddColumn(p => p.Name, new ContentView("Name".Underline()));
                table.AddColumn(p => p.AeTitle, new ContentView("AE Title".Underline()));
                table.AddColumn(p => p.Timeout, new ContentView("Timeout".Underline()));
                table.AddColumn(p => p.Grouping, new ContentView("Grouping".Underline()));
                table.AddColumn(p => p.Workflows.IsNullOrEmpty() ? "n/a" : string.Join(", ", p.Workflows), new ContentView("Workflows".Underline()));
                table.AddColumn(p => p.AllowedSopClasses.IsNullOrEmpty() ? "n/a" : string.Join(", ", p.AllowedSopClasses), new ContentView("Accepted SOP Classes".Underline()));
                table.AddColumn(p => p.IgnoredSopClasses.IsNullOrEmpty() ? "n/a" : string.Join(", ", p.IgnoredSopClasses), new ContentView("Ignored SOP Classes".Underline()));
                table.Render(consoleRenderer, consoleRegion.GetDefaultConsoleRegion());

                logger.ListedNItems(items.Count);
            }
            return ExitCodes.Success;
        }

        private async Task<int> RemoveAeTitlehandlerAsync(string name, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Deleting MONAI SCP AE Title {name}...");
                _ = await client.MonaiScpAeTitle.Delete(name, cancellationToken).ConfigureAwait(false);
                logger.MonaiAeTitleDeleted(name);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorDeletingMonaiAeTitle(name, ex.Message);
                return ExitCodes.MonaiScp_ErrorDelete;
            }
            return ExitCodes.Success;
        }

        private async Task<int> AddAeTitlehandlerAsync(MonaiApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(entity, nameof(entity));
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<AetCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);

                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                var result = await client.MonaiScpAeTitle.Create(entity, cancellationToken).ConfigureAwait(false);

                logger.MonaiAeTitleCreated(result.Name, result.AeTitle, result.Grouping, result.Timeout);

                if (result.Workflows.Any())
                {
                    logger.MonaiAeWorkflows(string.Join(',', result.Workflows));
                    logger.WorkflowWarning();
                }
                if (result.IgnoredSopClasses.Any())
                {
                    logger.MonaiAeIgnoredSops(string.Join(',', result.IgnoredSopClasses));
                    logger.IgnoredSopClassesWarning();
                }
                if (result.AllowedSopClasses.Any())
                {
                    logger.MonaiAeAllowedSops(string.Join(',', result.AllowedSopClasses));
                }
                if (result.PlugInAssemblies.Any())
                {
                    logger.MonaiAePlugIns(string.Join(',', result.PlugInAssemblies));
                }
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.MonaiAeCreateCritical(entity.AeTitle, ex.Message);
                return ExitCodes.MonaiScp_ErrorCreate;
            }
            return ExitCodes.Success;
        }

        private async Task<int> EditAeTitleHandlerAsync(MonaiApplicationEntity entity, IHost host, bool verbose, CancellationToken cancellationToken)
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
                LogVerbose(verbose, host, $"Updating SCP AE Title {entity.AeTitle}...");
                var result = await client.MonaiScpAeTitle.Update(entity, cancellationToken).ConfigureAwait(false);

                logger.MonaiAeTitleUpdated(result.Name, result.AeTitle, result.Grouping, result.Timeout);
                if (result.Workflows.Any())
                {
                    logger.MonaiAeWorkflows(string.Join(',', result.Workflows));
                    logger.WorkflowWarning();
                }
                if (result.IgnoredSopClasses.Any())
                {
                    logger.MonaiAeIgnoredSops(string.Join(',', result.IgnoredSopClasses));
                    logger.IgnoredSopClassesWarning();
                }
                if (result.AllowedSopClasses.Any())
                {
                    logger.MonaiAeAllowedSops(string.Join(',', result.AllowedSopClasses));
                    logger.AcceptedSopClassesWarning();
                }
                if (result.PlugInAssemblies.Any())
                {
                    logger.MonaiAePlugIns(string.Join(',', result.PlugInAssemblies));
                }
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorUpdatingMonaiApplicationEntity(entity.AeTitle, ex.Message);
                return ExitCodes.MonaiScp_ErrorUpdate;
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

            IDictionary<string, string>? items = null;
            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Retrieving MONAI SCP AE Titles...");
                items = await client.MonaiScpAeTitle.PlugIns(cancellationToken).ConfigureAwait(false);
            }
            catch (ConfigurationException ex)
            {
                logger.ConfigurationException(ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.ErrorListingDataInputPlugIns(ex.Message);
                return ExitCodes.MonaiScp_ErrorPlugIns;
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
