/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;

using FoDicomNetwork = FellowOakDicom.Network;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal abstract class ScpServiceBase : IHostedService, IDisposable, IMonaiService
    {
#pragma warning disable S2223 // Non-constant static fields should not be visible
        public static int ActiveConnections = 0;
#pragma warning restore S2223 // Non-constant static fields should not be visible

        private readonly IServiceScope _serviceScope;
        private readonly IApplicationEntityManager _associationDataProvider;
        private readonly ILogger<ScpServiceBase> _logger;
        private readonly ILogger<ScpServiceInternalBase> _scpServiceInternalLogger;
        protected readonly IHostApplicationLifetime AppLifetime;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        protected FoDicomNetwork.IDicomServer? Server;
        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public abstract string ServiceName { get; }

        public ScpServiceBase(IServiceScopeFactory serviceScopeFactory,
                                IApplicationEntityManager applicationEntityManager,
                                IHostApplicationLifetime appLifetime,
                                IOptions<InformaticsGatewayConfiguration> configuration)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(applicationEntityManager, nameof(applicationEntityManager));
            Guard.Against.Null(appLifetime, nameof(appLifetime));
            Guard.Against.Null(configuration, nameof(configuration));

            _serviceScope = serviceScopeFactory.CreateScope();

            var logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>();

            _logger = logginFactory!.CreateLogger<ScpServiceBase>();
            _scpServiceInternalLogger = logginFactory!.CreateLogger<ScpServiceInternal>();
            _associationDataProvider = applicationEntityManager;
            AppLifetime = appLifetime;
            _configuration = configuration;
            _ = DicomDictionary.Default;
        }

        public void Dispose()
        {
            _serviceScope.Dispose();
            Server?.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ServiceStart();
            return Task.CompletedTask;
        }

        public abstract void ServiceStart();

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            Server?.Stop();
            Server?.Dispose();
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        public void ServiceStartBase(int ScpPort)
        {
            try
            {
                _logger.ServiceStarting(ServiceName);
                Server = DicomServerFactory.Create<ScpServiceInternal>(
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
