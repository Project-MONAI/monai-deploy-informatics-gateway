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
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class StopCommand : CommandBase
    {
        public StopCommand() : base("stop", $"Stop the {Strings.ApplicationName} service")
        {
            AddConfirmationOption();
            Handler = CommandHandler.Create<IHost, bool, bool, CancellationToken>(StopCommandHandler);
        }

        private async Task<int> StopCommandHandler(IHost host, bool yes, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            var service = host.Services.GetRequiredService<IControlService>();
            var confirmation = host.Services.GetRequiredService<IConfirmationPrompt>();
            var logger = CreateLogger<StopCommand>(host);

            Guard.Against.Null(service, nameof(service), "Control service is unavailable.");
            Guard.Against.Null(confirmation, nameof(confirmation), "Confirmation prompt is unavailable.");
            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");

            if (!yes && !confirmation.ShowConfirmationPrompt($"Do you want to stop {Strings.ApplicationName}?"))
            {
                logger.Log(LogLevel.Warning, "Action cancelled.");
                return ExitCodes.Stop_Cancelled;
            }

            try
            {
                await service.StopService(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, ex, ex.Message);
                return ExitCodes.Stop_Error;
            }
            return ExitCodes.Success;
        }
    }
}
