// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConfigCommand : CommandBase
    {
        public ConfigCommand() : base("config", "Configure the CLI endpoint")
        {
            AddCommandEndpoint();
            AddCommandRunner();

            SetupInitCommand();
            SetupShowConfigCommand();
        }

        private void AddCommandRunner()
        {
            var endpointCommand = new Command("runner", $"Default container runner/orchestration engine to run {Strings.ApplicationName}.");
            Add(endpointCommand);

            endpointCommand.AddArgument(new Argument<Runner>("runner"));
            endpointCommand.Handler = CommandHandler.Create<Runner, IHost, bool>((Runner runner, IHost host, bool verbose) =>
                ConfigUpdateHandler(runner, host, verbose, (IConfigurationService options) =>
                {
                    options.Configurations.Runner = runner;
                })
            );
        }

        private void AddCommandEndpoint()
        {
            var endpointCommand = new Command("endpoint", $"URL to the {Strings.ApplicationName} API. E.g. http://localhost:5000");
            Add(endpointCommand);

            endpointCommand.AddArgument(new Argument<string>("uri"));
            endpointCommand.Handler = CommandHandler.Create<string, IHost, bool>((string uri, IHost host, bool verbose) =>
                ConfigUpdateHandler(uri, host, verbose, (IConfigurationService options) =>
                {
                    options.Configurations.InformaticsGatewayServerEndpoint = uri;
                })
            );
        }

        private void SetupInitCommand()
        {
            var listCommand = new Command("init", $"Initialize with default configuration options");
            AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<IHost, bool, bool, CancellationToken>(InitHandlerAsync);
            AddConfirmationOption(listCommand);
        }

        private void SetupShowConfigCommand()
        {
            var showCommand = new Command("show", "Show configurations");
            AddCommand(showCommand);

            showCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ShowConfigurationHandler);
        }

        private int ShowConfigurationHandler(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");
            var logger = CreateLogger<ConfigCommand>(host);
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");

            try
            {
                CheckConfiguration(configService);
                logger.Log(LogLevel.Information, $"Informatics Gateway API: {configService.Configurations.InformaticsGatewayServerEndpoint}");
                logger.Log(LogLevel.Information, $"DICOM SCP Listening Port: {configService.Configurations.DicomListeningPort}");
                logger.Log(LogLevel.Information, $"Container Runner: {configService.Configurations.Runner}");
                logger.Log(LogLevel.Information, $"Host:");
                logger.Log(LogLevel.Information, $"   Database storage mount: {configService.Configurations.HostDatabaseStorageMount}");
                logger.Log(LogLevel.Information, $"   Data storage mount: {configService.Configurations.HostDataStorageMount}");
                logger.Log(LogLevel.Information, $"   Logs storage mount: {configService.Configurations.HostLogsStorageMount}");
            }
            catch (ConfigurationException)
            {
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.Message);
                return ExitCodes.Config_ErrorShowing;
            }
            return ExitCodes.Success;
        }

        private static int ConfigUpdateHandler<T>(T argument, IHost host, bool verbose, Action<IConfigurationService> updater)
        {
            Guard.Against.Null(host, nameof(host));
            Guard.Against.Null(updater, nameof(updater));

            var logger = CreateLogger<ConfigCommand>(host);
            var config = host.Services.GetRequiredService<IConfigurationService>();

            Guard.Against.Null(config, nameof(config), "Configuration service is unavailable.");

            try
            {
                CheckConfiguration(config);
                updater(config);
                logger.Log(LogLevel.Information, "Configuration updated successfully.");
            }
            catch (ConfigurationException)
            {
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.Message);
                return ExitCodes.Config_ErrorSaving;
            }
            return ExitCodes.Success;
        }

        private async Task<int> InitHandlerAsync(IHost host, bool verbose, bool yes, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            var logger = CreateLogger<ConfigCommand>(host);
            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var confirmation = host.Services.GetRequiredService<IConfirmationPrompt>();
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(confirmation, nameof(confirmation), "Confirmation prompt is unavailable.");

            if (!yes)
            {
                if (configService.IsConfigExists && !confirmation.ShowConfirmationPrompt($"Existing application configuration file already exists. Do you want to overwrite it?"))
                {
                    logger.Log(LogLevel.Warning, "Action cancelled.");
                    return ExitCodes.Stop_Cancelled;
                }
            }

            try
            {
                await configService.Initialize(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.Message);
                return ExitCodes.Config_ErrorInitializing;
            }
            return ExitCodes.Success;
        }
    }
}
