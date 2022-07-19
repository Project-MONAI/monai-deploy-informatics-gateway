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
