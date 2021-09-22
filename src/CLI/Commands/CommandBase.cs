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
using Crayon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client;
using System;
using System.CommandLine;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class CommandBase : Command
    {
        public CommandBase(string name, string description) : base(name, description)
        {
        }

        protected ILogger CreateLogger<T>(IHost host)
        {
            var loggerFactory = host.Services.GetService<ILoggerFactory>();
            return loggerFactory?.CreateLogger<T>();
        }

        protected void LogVerbose(bool verbose, IHost host, string message)
        {
            if (verbose)
            {
                var logger = CreateLogger<CommandBase>(host);
                if (logger is null)
                {
                    Console.Out.WriteLineAsync(Output.Red(message));
                }
                else
                {
                    logger.Log(LogLevel.Debug, message);
                }
            }
        }

        protected ConfigurationOptions LoadConfiguration(bool verbose, IConfigurationService configurationService, IInformaticsGatewayClient client)
        {
            Guard.Against.Null(configurationService, nameof(configurationService));
            Guard.Against.Null(client, nameof(client));

            var configuration = LoadConfiguration(verbose, configurationService);
            client.ConfigureServiceUris(new Uri(configuration.Endpoint));
            return configuration;
        }

        protected ConfigurationOptions LoadConfiguration(bool verbose, IConfigurationService configurationService)
        {
            Guard.Against.Null(configurationService, nameof(configurationService));

            if (configurationService.ConfigurationExists())
            {
                var config = configurationService.Load(verbose);
                return config;
            }

            throw new ConfigurationException($"{Strings.ApplicationName} endpoint not configured.  Please run 'config` first.");
        }

        protected void AddConfirmationOption()
        {
            var confirmationOption = new Option<bool>(new[] { "-y", "--yes" }, "Automatic yes to prompts");
            this.AddOption(confirmationOption);
        }
    }
}
