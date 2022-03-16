// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class StartCommand : CommandBase
    {
        public StartCommand() : base("start", $"Start the {Strings.ApplicationName} service")
        {
            AddConfirmationOption();
            Handler = CommandHandler.Create<IHost, bool, CancellationToken>(StartCommandHandler);
        }

        private async Task<int> StartCommandHandler(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            var service = host.Services.GetRequiredService<IControlService>();
            var confirmation = host.Services.GetRequiredService<IConfirmationPrompt>();
            var logger = CreateLogger<StartCommand>(host);

            Guard.Against.Null(confirmation, nameof(confirmation), "Confirmation prompt is unavailable.");
            Guard.Against.Null(service, nameof(service), "Control service is unavailable.");
            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");

            try
            {
                await service.StartService(cancellationToken).ConfigureAwait(false);
            }
            catch (ControlException ex) when (ex.ErrorCode == ExitCodes.Start_Error_ApplicationAlreadyRunning)
            {
                logger.WarningMessage(ex.Message);
                return ex.ErrorCode;
            }
            catch (Exception ex)
            {
                logger.CriticalException(ex.Message);
                return ExitCodes.Start_Error;
            }
            return ExitCodes.Success;
        }
    }
}
