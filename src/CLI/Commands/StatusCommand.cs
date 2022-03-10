// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class StatusCommand : CommandBase
    {
        public StatusCommand() : base("status", $"{Strings.ApplicationName} service status")
        {
            Handler = CommandHandler.Create<IHost, bool, CancellationToken>(StatusCommandHandlerAsync);
        }

        private async Task<int> StatusCommandHandlerAsync(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            LogVerbose(verbose, host, "Configuring services...");

            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<IInformaticsGatewayClient>();
            var logger = CreateLogger<StatusCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), $"{Strings.ApplicationName} client is unavailable.");

            HealthStatusResponse response;
            try
            {
                CheckConfiguration(configService);
                client.ConfigureServiceUris(configService.Configurations.InformaticsGatewayServerUri);

                LogVerbose(verbose, host, $"Connecting to {Strings.ApplicationName} at {configService.Configurations.InformaticsGatewayServerEndpoint}...");
                LogVerbose(verbose, host, $"Retrieving service status...");
                response = await client.Health.Status(cancellationToken);
            }
            catch (ConfigurationException ex)
            {
                logger.Log(LogLevel.Critical, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error retrieving service status: {ex.Message}");
                return ExitCodes.Status_Error;
            }

            logger.Log(LogLevel.Information, $"Number of active DIMSE connections: {response.ActiveDimseConnections}");
            logger.Log(LogLevel.Information, "Service Status: ");
            foreach (var service in response.Services.Keys)
            {
                logger.Log(LogLevel.Information, $"\t\t{service}: {response.Services[service]}");
            }
            return ExitCodes.Success;
        }
    }
}
