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
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class StopCommand : CommandBase
    {
        public StopCommand() : base("stop", $"Stop the {Strings.ApplicationName} service")
        {
            this.AddConfirmationOption();
            this.Handler = CommandHandler.Create<IHost, bool, bool, CancellationToken>(StopCommandHandler);
        }

        private async Task<int> StopCommandHandler(IHost host, bool yes, bool verbose, CancellationToken cancellationToken)
        {
            var service = host.Services.GetRequiredService<IControlService>();
            var confirmation = host.Services.GetRequiredService<IConfirmationPrompt>();
            var logger = CreateLogger<StopCommand>(host);

            Guard.Against.Null(service, nameof(service), "Control service is unavailable.");
            Guard.Against.Null(confirmation, nameof(confirmation), "Confirmation prompt is unavailable.");
            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");

            if (!yes)
            {
                if (!confirmation.ShowConfirmationPrompt($"Do you want to stop {Strings.ApplicationName}?"))
                {
                    logger.Log(LogLevel.Warning, "Action cancelled.");
                    return ExitCodes.Stop_Cancelled;
                }
            }

            try
            {
                await service.Stop(cancellationToken);
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
