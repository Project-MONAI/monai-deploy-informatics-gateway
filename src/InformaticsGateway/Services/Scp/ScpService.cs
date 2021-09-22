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

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Dicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FoDicomNetwork = Dicom.Network;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    public class ScpService : IHostedService, IDisposable, IMonaiService
    {
        internal static int ActiveConnections = 0;
        private readonly IServiceScope _serviceScope;
        private readonly IApplicationEntityManager _associationDataProvider;
        private readonly ILogger<ScpService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private FoDicomNetwork.IDicomServer _server;
        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public string ServiceName => "DICOM SCP Service";

        public ScpService(IServiceScopeFactory serviceScopeFactory,
                                IApplicationEntityManager applicationEntityManager,
                                IHostApplicationLifetime appLifetime,
                                IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScope = serviceScopeFactory.CreateScope();
            _associationDataProvider = applicationEntityManager ?? throw new ArgumentNullException(nameof(applicationEntityManager));

            var logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>().CaptureFoDicomLogs();

            _logger = logginFactory.CreateLogger<ScpService>();
            _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var preloadDictionary = DicomDictionary.Default;
        }

        public void Dispose()
        {
            _serviceScope.Dispose();
            _server?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, "MONAI Deploy Informatics Gateway (SCP Service) {0} loading...",
                Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version);

            try
            {
                _logger.Log(LogLevel.Information, "Starting SCP Service.");
                var options = new FoDicomNetwork.DicomServiceOptions
                {
                    IgnoreUnsupportedTransferSyntaxChange = true,
                    LogDimseDatasets = _configuration.Value.Dicom.Scp.LogDimseDatasets,
                    MaxClientsAllowed = _configuration.Value.Dicom.Scp.MaximumNumberOfAssociations
                };

                _server = FoDicomNetwork.DicomServer.Create<ScpServiceInternal>(
                    FoDicomNetwork.NetworkManager.IPv4Any,
                    _configuration.Value.Dicom.Scp.Port,
                    options: options,
                    userState: _associationDataProvider);

                if (_server.Exception != null)
                {
                    _logger.Log(LogLevel.Critical, _server.Exception, "Failed to initialize SCP listener.");
                    throw _server.Exception;
                }

                Status = ServiceStatus.Running;
                _logger.Log(LogLevel.Information, "SCP listening on port: {0}", _configuration.Value.Dicom.Scp.Port);
            }
            catch (System.Exception ex)
            {
                Status = ServiceStatus.Cancelled;
                _logger.Log(LogLevel.Critical, ex, "Failed to start SCP listener.");
                _appLifetime.StopApplication();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Information, "Stopping SCP Service.");
            _server?.Stop();
            _server?.Dispose();
            Status = ServiceStatus.Stopped;
            _logger.Log(LogLevel.Information, "SCP Service stopped.");
            return Task.CompletedTask;
        }
    }
}
