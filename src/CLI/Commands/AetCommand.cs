// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
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
            SetupRemoveAetCommand();
            SetupListAetCommand();
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
            var workflowsOption = new Option<string[]>(new string[] { "-w", "--workflows" }, () => Array.Empty<string>(), "A space separated list of workflow names or IDs to be associated with the SCP AE Title")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
                Name = "--workflows"
            };
            addCommand.AddOption(workflowsOption);
            var ignoredSopsOption = new Option<string[]>(new string[] { "-i", "--ignored-sops" }, () => Array.Empty<string>(), "A space separated list of SOP Class UIDs to be ignoredS")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = false,
                Name = "--ignored-sops"
            };
            addCommand.AddOption(ignoredSopsOption);

            addCommand.Handler = CommandHandler.Create<MonaiApplicationEntity, IHost, bool, CancellationToken>(AddAeTitlehandlerAsync);
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

            IReadOnlyList<MonaiApplicationEntity> items = null;
            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);
                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Retrieving MONAI SCP AE Titles...");
                items = await client.MonaiScpAeTitle.List(cancellationToken);
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error retrieving MONAI SCP AE Titles: {ex.Message}");
                return ExitCodes.MonaiScp_ErrorList;
            }

            if (items.IsNullOrEmpty())
            {
                logger.Log(LogLevel.Warning, "No MONAI SCP Application Entities configured.");
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
                table.AddColumn(p => p.Workflows.IsNullOrEmpty() ? "n/a" : string.Join(", ", p.Workflows), new ContentView("Workflows".Underline()));
                table.Render(consoleRenderer, consoleRegion.GetDefaultConsoleRegion());
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
                _ = await client.MonaiScpAeTitle.Delete(name, cancellationToken);
                logger.Log(LogLevel.Information, $"MONAI SCP AE Title '{name}' deleted.");
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error deleting MONAI SCP AE Title {name}: {ex.Message}");
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
                var result = await client.MonaiScpAeTitle.Create(entity, cancellationToken);

                logger.Log(LogLevel.Information, "New MONAI Deploy SCP Application Entity created:");
                logger.Log(LogLevel.Information, $"\tName:     {result.Name}");
                logger.Log(LogLevel.Information, $"\tAE Title: {result.AeTitle}");
                logger.Log(LogLevel.Information, $"\tGrouping: {result.Grouping}");
                logger.Log(LogLevel.Information, $"\tTimeout: {result.Grouping}s");

                if (result.Workflows.Any())
                {
                    logger.Log(LogLevel.Information, $"\tWorkflows:{string.Join(',', result.Workflows)}");
                    logger.Log(LogLevel.Warning, "Data received by this Application Entity will bypass Data Routing Service.");
                }
                if (result.IgnoredSopClasses.Any())
                {
                    logger.Log(LogLevel.Information, $"\tIgnored SOP Classes:{string.Join(',', result.IgnoredSopClasses)}");
                    logger.Log(LogLevel.Warning, "Instances with matching SOP class UIDs are accepted but dropped.");
                }
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error creating MONAI SCP AE Title {entity.AeTitle}: {ex.Message}");
                return ExitCodes.MonaiScp_ErrorCreate;
            }
            return ExitCodes.Success;
        }
    }
}
