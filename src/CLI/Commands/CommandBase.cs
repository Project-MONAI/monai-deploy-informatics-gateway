/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.CommandLine;
using Ardalis.GuardClauses;
using Crayon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class CommandBase : Command
    {
        public CommandBase(string name, string description) : base(name, description)
        {
        }

        protected static ILogger CreateLogger<T>(IHost host)
        {
            Guard.Against.Null(host, nameof(host));

            var loggerFactory = host.Services.GetService<ILoggerFactory>();
            return loggerFactory?.CreateLogger<T>();
        }

        protected static void LogVerbose(bool verbose, IHost host, string message)
        {
            Guard.Against.Null(host, nameof(host));
            Guard.Against.NullOrWhiteSpace(message, nameof(message));

            if (verbose)
            {
                var logger = CreateLogger<CommandBase>(host);
                if (logger is null)
                {
                    Console.Out.WriteLineAsync(Output.Red(message));
                }
                else
                {
                    logger.DebugMessage(message);
                }
            }
        }

        protected void AddConfirmationOption() => AddConfirmationOption(this);

        protected static void AddConfirmationOption(Command command)
        {
            Guard.Against.Null(command, nameof(command));

            var confirmationOption = new Option<bool>(new[] { "-y", "--yes" }, "Automatic yes to prompts");
            command.AddOption(confirmationOption);
        }

        protected static void CheckConfiguration(IConfigurationService configService)
        {
            Guard.Against.Null(configService, nameof(configService));

            if (!configService.IsInitialized)
            {
                throw new ConfigurationException($"Please execute `{AppDomain.CurrentDomain.FriendlyName} config init` to intialize Informatics Gateway.");
            }
        }
    }
}
