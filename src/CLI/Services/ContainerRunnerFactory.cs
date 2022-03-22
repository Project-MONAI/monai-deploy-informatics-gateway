// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public class ContainerRunnerFactory : IContainerRunnerFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfigurationService _configurationService;

        public ContainerRunnerFactory(IServiceScopeFactory serviceScopeFactory, IConfigurationService configurationService)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new System.ArgumentNullException(nameof(serviceScopeFactory));
            _configurationService = configurationService ?? throw new System.ArgumentNullException(nameof(configurationService));
        }

        public IContainerRunner GetContainerRunner()
        {
            var scope = _serviceScopeFactory.CreateScope();
            return _configurationService.Configurations.Runner switch
            {
                Runner.Docker => scope.ServiceProvider.GetRequiredService<DockerRunner>(),
                _ => throw new NotImplementedException($"The configured runner isn't yet supported '{_configurationService.Configurations.Runner}'"),
            };
        }
    }
}
