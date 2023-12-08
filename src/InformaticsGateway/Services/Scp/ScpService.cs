/*
 * Copyright 2021-2023 MONAI Consortium
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

using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ScpService : ScpServiceBase
    {
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<ScpService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;

        public override string ServiceName => "DICOM SCP Service";

        public ScpService(IServiceScopeFactory serviceScopeFactory,
                                IApplicationEntityManager applicationEntityManager,
                                IHostApplicationLifetime appLifetime,
                                IOptions<InformaticsGatewayConfiguration> configuration) : base(serviceScopeFactory, applicationEntityManager, appLifetime, configuration)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(applicationEntityManager, nameof(applicationEntityManager));
            Guard.Against.Null(appLifetime, nameof(appLifetime));
            Guard.Against.Null(configuration, nameof(configuration));

            _serviceScope = serviceScopeFactory.CreateScope();
            var logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>();

            _logger = logginFactory!.CreateLogger<ScpService>();
            _configuration = configuration;
        }

        public override void ServiceStart()
        {
            _logger.AddingScpListener(ServiceName, _configuration.Value.Dicom.Scp.Port);
            ServiceStartBase(_configuration.Value.Dicom.Scp.Port);
        }
    }
}
