// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IControlService
    {
        Task RestartService(CancellationToken cancellationToken = default);

        Task StartService(CancellationToken cancellationToken = default);

        Task StopService(CancellationToken cancellationToken = default);
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

        public async Task RestartService(CancellationToken cancellationToken = default)
        {
            await StopService(cancellationToken).ConfigureAwait(false);
            await StartService(cancellationToken).ConfigureAwait(false);
        }

        public async Task StartService(CancellationToken cancellationToken = default)
        {
            var runner = _containerRunnerFactory.GetContainerRunner();

            var applicationVersion = await runner.GetLatestApplicationVersion(cancellationToken).ConfigureAwait(false);
            if (applicationVersion is null)
            {
                throw new ControlException(ExitCodes.Start_Error_ApplicationNotFound, $"No {Strings.ApplicationName} Docker images with prefix `{_configurationService.Configurations.DockerImagePrefix}` found.");
            }
            var runnerState = await runner.IsApplicationRunning(applicationVersion, cancellationToken).ConfigureAwait(false);

            if (runnerState.IsRunning)
            {
                throw new ControlException(ExitCodes.Start_Error_ApplicationAlreadyRunning, $"{Strings.ApplicationName} is already running in container ID {runnerState.IdShort}.");
            }

            await runner.StartApplication(applicationVersion, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Stops any running applications, including, previous releases/versions.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task StopService(CancellationToken cancellationToken = default)
        {
            var runner = _containerRunnerFactory.GetContainerRunner();
            var applicationVersions = await runner.GetApplicationVersions(cancellationToken).ConfigureAwait(false);

            if (!applicationVersions.IsNullOrEmpty())
            {
                foreach (var applicationVersion in applicationVersions)
                {
                    var runnerState = await runner.IsApplicationRunning(applicationVersion, cancellationToken).ConfigureAwait(false);

                    _logger.ApplicationStoppedState(Strings.ApplicationName, runnerState.Id, runnerState.IsRunning);
                    if (runnerState.IsRunning)
                    {
                        if (await runner.StopApplication(runnerState, cancellationToken).ConfigureAwait(false))
                        {
                            _logger.ApplicationStopped(Strings.ApplicationName, runnerState.Id);
                        }
                        else
                        {
                            _logger.ApplicationStopError(Strings.ApplicationName, runnerState.Id, _configurationService.Configurations.Runner);
                        }
                    }
                }
            }
        }
    }
}
