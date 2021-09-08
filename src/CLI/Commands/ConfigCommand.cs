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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class ConfigCommand : CommandBase
    {
        public ConfigCommand() : base("config", "Configure the CLI endpoint")
        {
            var endpointOption = new Option<string>(new[] { "-e", "--endpoint" }, $"URL to the {Strings.ApplicationName} API. E.g. http://localhost:5000") { IsRequired = true };
            this.AddOption(endpointOption);

            this.Handler = CommandHandler.Create<ConfigurationOptions, IHost, bool>(ConfigCommandHandler);

            SetupShowConfigCmmand();
        }

        private void SetupShowConfigCmmand()
        {
            var showCommand = new Command("show", "Show configurations");
            this.AddCommand(showCommand);

            showCommand.Handler = CommandHandler.Create<IHost, bool, CancellationToken>(ShowConfiguratonHandler);
        }

        private int ShowConfiguratonHandler(IHost host, bool verbose, CancellationToken cancellationToken)
        {
            this.LogVerbose(verbose, host, "Configuring services...");
            var logger = CreateLogger<ConfigCommand>(host);
            var configService = host.Services.GetRequiredService<IConfigurationService>();

            try
            {
                var service = host.Services.GetRequiredService<IConfigurationService>();
                ConfigurationOptions config = LoadConfiguration(verbose, configService);

                logger.Log(LogLevel.Information, $"Endpoint: {config.Endpoint}");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.Message);
                return ExitCodes.Config_NotConfigured;
            }
            return ExitCodes.Success;
        }

        private int ConfigCommandHandler(ConfigurationOptions options, IHost host, bool verbose)
        {
            var logger = CreateLogger<ConfigCommand>(host);

            try
            {
                options.Validate();
                var service = host.Services.GetRequiredService<IConfigurationService>();
                service.CreateConfigDirectoryIfNotExist();

                var configuration = service.Load(verbose);
                configuration.Endpoint = options.Endpoint;
                service.Save(configuration);
            }
            catch (ArgumentNullException)
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
    }
}
