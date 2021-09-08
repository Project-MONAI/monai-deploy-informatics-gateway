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
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class StartCommand : CommandBase
    {
        public StartCommand() : base("start", $"Start the {Strings.ApplicationName} service")
        {
            this.AddConfirmationOption();
            this.Handler = CommandHandler.Create<IHost, bool, bool>(StartCommandHandler);
        }

        private async Task<int> StartCommandHandler(IHost host, bool yes, bool verbose)
        {
            var service = host.Services.GetRequiredService<IControlService>();
            var confirmation = host.Services.GetRequiredService<IConfirmationPrompt>();
            var logger = CreateLogger<StartCommand>(host);

            Guard.Against.Null(confirmation, nameof(confirmation), "Confirmation prompt is unavailable.");
            Guard.Against.Null(service, nameof(service), "Control service is unavailable.");
            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");

            if (!yes)
            {
                if (!confirmation.ShowConfirmationPrompt($"Do you want to restart {Strings.ApplicationName}?"))
                {
                    logger.Log(LogLevel.Warning, "Action cancelled.");
                    return ExitCodes.Start_Cancelled;
                }
            }

            try
            {
                await service.Start();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Critical, $"Error starting {Strings.ApplicationName}: {ex.Message}");
                return ExitCodes.Start_Error;
            }
            return ExitCodes.Success;
        }
    }
}
