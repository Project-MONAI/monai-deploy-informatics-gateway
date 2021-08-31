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
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Client;
using System;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class StatusCommand : CommandBase
    {
        public StatusCommand() : base("status", "MONAI Informatics Gateway service status")
        {
            this.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(StatusCommandHandlerAsync);
        }

        private async Task StatusCommandHandlerAsync(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");

            var configService = host.Services.GetRequiredService<IConfigurationService>();
            var client = host.Services.GetRequiredService<InformaticsGatewayClient>();
            var logger = CreateLogger<StatusCommand>(host);

            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");
            Guard.Against.Null(configService, nameof(configService), "Configuration service is unavailable.");
            Guard.Against.Null(client, nameof(client), "Informatics Gateway client is unavailable.");

            HealthStatusResponse response = null;
            try
            {
                ConfigurationOptions config = LoadConfiguration(verbose, configService, client);

                this.LogVerbose(verbose, host, $"Connecting to Informatics Gateway at {config.Endpoint}...");
                this.LogVerbose(verbose, host, $"Retrieving service status...");
                response = await client.Health.Status(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error retrieving service status: {ex.Message}");
                return;
            }

            logger.Log(LogLevel.Information, $"Number of active DIMSE connections: {response.ActiveDimseConnections}");
            logger.Log(LogLevel.Information, "Service Status: ");
            foreach (var service in response.Services.Keys)
            {
                logger.Log(LogLevel.Information, $"\t\t{service}: {response.Services[service]}");
            }
        }
    }
}