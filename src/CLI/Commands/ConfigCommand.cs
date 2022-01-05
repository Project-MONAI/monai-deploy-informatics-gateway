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
using Monai.Deploy.InformaticsGateway.CLI.Services;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConfigCommand : CommandBase
    {
        public ConfigCommand() : base("config", "Configure the CLI endpoint")
        {
            AddCommandEndpoint();
            AddCommandRunner();
            AddCommandWorkloadManagerRest();
            AddCommandWorkloadManagerGrpc();

            SetupInitCommand();
            SetupShowConfigCommand();
        }

        private void AddCommandWorkloadManagerGrpc()
        {
            var wmgrpcCommand = new Command("wmgrpc", $"RESTful endpoint for the {Strings.WorkloadManagerName}.");
            this.Add(wmgrpcCommand);

            wmgrpcCommand.AddArgument(new Argument<string>("uri"));
            wmgrpcCommand.Handler = CommandHandler.Create<string, IHost, bool>((string uri, IHost host, bool verbose) =>
                ConfigUpdateHandler(uri, host, verbose, (IConfigurationService options) =>
                {
                    options.Configurations.WorkloadManagerGrpcEndpoint = uri;
                })
            );
        }

        private void AddCommandWorkloadManagerRest()
        {
            var wmRestCommand = new Command("wmrest", $"RESTful endpoint for the {Strings.WorkloadManagerName}.");
            this.Add(wmRestCommand);

            wmRestCommand.AddArgument(new Argument<string>("uri"));
            wmRestCommand.Handler = CommandHandler.Create<string, IHost, bool>((string uri, IHost host, bool verbose) =>
                ConfigUpdateHandler(uri, host, verbose, (IConfigurationService options) =>
                {
                    options.Configurations.WorkloadManagerRestEndpoint = uri;
                })
            );
        }

        private void AddCommandRunner()
        {
            var endpointCommand = new Command("runner", $"Default container runner/orchestration engine to run {Strings.ApplicationName}.");
            this.Add(endpointCommand);

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
            this.Add(endpointCommand);

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
            this.AddCommand(listCommand);

            listCommand.Handler = CommandHandler.Create<IHost, bool, bool, CancellationToken>(InitHandlerAsync);
            this.AddConfirmationOption(listCommand);
        }

        private void SetupShowConfigCommand()
        {
            var showCommand = new Command("show", "Show configurations");
            this.AddCommand(showCommand);

            showCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ShowConfigurationHandler);
        }

        private int ShowConfigurationHandler(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            this.LogVerbose(verbose, host, "Configuring services...");
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
                logger.Log(LogLevel.Information, $"Workload Manager:");
                logger.Log(LogLevel.Information, $"   REST API: {configService.Configurations.WorkloadManagerRestEndpoint}");
                logger.Log(LogLevel.Information, $"   gRPC API: {configService.Configurations.WorkloadManagerGrpcEndpoint}");
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

        private int ConfigUpdateHandler<T>(T argument, IHost host, bool verbose, Action<IConfigurationService> updater)
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
