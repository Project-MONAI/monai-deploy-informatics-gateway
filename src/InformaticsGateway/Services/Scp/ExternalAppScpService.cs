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
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;


namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ExternalAppScpService : ScpServiceBase
    {
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<ExternalAppScpService> _logger;
        private readonly ILogger<ExternalAppScpServiceInternal> _scpServiceInternalLogger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IApplicationEntityManager _associationDataProvider;

        public override string ServiceName => "External App DICOM SCP Service";

        public ExternalAppScpService(IServiceScopeFactory serviceScopeFactory,
                                IApplicationEntityManager applicationEntityManager,
                                IHostApplicationLifetime appLifetime,
                                IOptions<InformaticsGatewayConfiguration> configuration) : base(serviceScopeFactory, applicationEntityManager, appLifetime, configuration)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(applicationEntityManager, nameof(applicationEntityManager));
            Guard.Against.Null(appLifetime, nameof(appLifetime));
            Guard.Against.Null(configuration, nameof(configuration));

            _associationDataProvider = applicationEntityManager;

            _serviceScope = serviceScopeFactory.CreateScope();
            var logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>();

            _logger = logginFactory!.CreateLogger<ExternalAppScpService>();
            _scpServiceInternalLogger = logginFactory!.CreateLogger<ExternalAppScpServiceInternal>();
            _configuration = configuration;
        }

        public override void ServiceStart()
        {
            var ScpPort = _configuration.Value.Dicom.Scp.ExternalAppPort;
            try
            {
                _logger.AddingScpListener(ServiceName, ScpPort);
                Server = DicomServerFactory.Create<ExternalAppScpServiceInternal>(
                    NetworkManager.IPv4Any,
                    ScpPort,
                    logger: _scpServiceInternalLogger,
                    userState: _associationDataProvider,
                    configure: configure => configure.MaxClientsAllowed = _configuration.Value.Dicom.Scp.MaximumNumberOfAssociations);

                Server.Options.IgnoreUnsupportedTransferSyntaxChange = true;
                Server.Options.LogDimseDatasets = _configuration.Value.Dicom.Scp.LogDimseDatasets;

                if (Server.Exception != null)
                {
                    _logger.ScpListenerInitializationFailure();
                    throw Server.Exception;
                }

                Status = ServiceStatus.Running;
                _logger.ScpListeningOnPort(ServiceName, ScpPort);
            }
            catch (System.Exception ex)
            {
                Status = ServiceStatus.Cancelled;
                _logger.ServiceFailedToStart(ServiceName, ex);
                AppLifetime.StopApplication();
            }

        }
    }
}
