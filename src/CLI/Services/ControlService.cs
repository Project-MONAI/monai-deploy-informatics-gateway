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
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public interface IControlService
    {
        Task Start(CancellationToken cancellationToken = default);

        Task Stop(CancellationToken cancellationToken = default);

        Task Restart(CancellationToken cancellationToken = default);
    }

    public class ControlService : IControlService
    {
        private readonly IContainerRunnerFactory _containerRunnerFactory;
        private readonly ILogger<ControlService> _logger;
        private readonly IConfigurationService _configurationService;

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

            var applicationVersion = await runner.GetApplicationVersion(cancellationToken);
            var runnerState = await runner.IsApplicationRunning(applicationVersion, cancellationToken);

            if (runnerState.IsRunning)
            {
                throw new Exception($"{Strings.ApplicationName} is already running in container ID {runnerState.IdShort}.");
            }

            await runner.StartApplication(applicationVersion, cancellationToken);
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            var runner = _containerRunnerFactory.GetContainerRunner();
            var applicationVersions = await runner.GetApplicationVersions(cancellationToken);

            foreach (var applicationVersion in applicationVersions)
            {
                var runnerState = await runner.IsApplicationRunning(applicationVersion, cancellationToken);

                if (runnerState.IsRunning)
                {
                    await runner.StopApplication(runnerState, cancellationToken);
                    return;
                }
            }
            _logger.Log(LogLevel.Warning, $"{Strings.ApplicationName} has not started. To start, execute `{System.AppDomain.CurrentDomain.FriendlyName} start`.");
        }
    }
}
