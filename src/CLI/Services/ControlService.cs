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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IControlService
    {
        Task Restart(CancellationToken cancellationToken = default);

        Task Start(CancellationToken cancellationToken = default);

        Task Stop(CancellationToken cancellationToken = default);
    }

    public class ControlService : IControlService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IContainerRunnerFactory _containerRunnerFactory;
        private readonly ILogger<ControlService> _logger;

        public ControlService(IContainerRunnerFactory containerRunnerFactory, ILogger<ControlService> logger, IConfigurationService configService)
        {
            _containerRunnerFactory = containerRunnerFactory ?? throw new ArgumentNullException(nameof(containerRunnerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public async Task Restart(CancellationToken cancellationToken = default)
        {
            await Stop();
            await Start(cancellationToken);
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            var runner = _containerRunnerFactory.GetContainerRunner();

            var applicationVersion = await runner.GetLatestApplicationVersion(cancellationToken);
            if (applicationVersion is null)
            {
                throw new ControlException(ExitCodes.Start_Error_ApplicationNotFound, $"No {Strings.ApplicationName} Docker images with prefix `{_configurationService.Configurations.DockerImagePrefix}` found.");
            }
            var runnerState = await runner.IsApplicationRunning(applicationVersion, cancellationToken);

            if (runnerState.IsRunning)
            {
                throw new ControlException(ExitCodes.Start_Error_ApplicationAlreadyRunning, $"{Strings.ApplicationName} is already running in container ID {runnerState.IdShort}.");
            }

            await runner.StartApplication(applicationVersion, cancellationToken);
        }

        /// <summary>
        /// Stops any running applications, including, previous releases/versions.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task Stop(CancellationToken cancellationToken = default)
        {
            var runner = _containerRunnerFactory.GetContainerRunner();
            var applicationVersions = await runner.GetApplicationVersions(cancellationToken);

            if (!applicationVersions.IsNullOrEmpty())
            {
                foreach (var applicationVersion in applicationVersions)
                {
                    var runnerState = await runner.IsApplicationRunning(applicationVersion, cancellationToken);

                    _logger.Log(LogLevel.Debug, $"{Strings.ApplicationName} with container ID {runnerState.Id} running={runnerState.IsRunning}.");
                    if (runnerState.IsRunning)
                    {
                        if (await runner.StopApplication(runnerState, cancellationToken))
                        {
                            _logger.Log(LogLevel.Information, $"{Strings.ApplicationName} with container ID {runnerState.Id} stopped.");
                        }
                        else
                        {
                            _logger.Log(LogLevel.Warning, $"Error may have occurred stopping {Strings.ApplicationName} with container ID {runnerState.Id}. Please verify with the applicatio state with {_configurationService.Configurations.Runner}.");
                        }
                    }
                }
            }
        }
    }
}
