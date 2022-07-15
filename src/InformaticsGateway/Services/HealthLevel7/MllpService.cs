// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal sealed class MllpService : IHostedService, IDisposable, IMonaiService
    {
        private const int SOCKET_OPERATION_CANCELLED = 125;
        private bool _disposedValue;
        private readonly ITcpListener _tcpListener;
        private readonly IMllpClientFactory _mllpClientFactory;
        private readonly IServiceScope _serviceScope;
        private readonly ILoggerFactory _logginFactory;
        private readonly ILogger<MllpService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly ConcurrentDictionary<Guid, IMllpClient> _activeTasks;

        public int ActiveConnections
        {
            get
            {
                return _activeTasks.Count;
            }
        }

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public string ServiceName => "HL7 Service";

        public MllpService(IServiceScopeFactory serviceScopeFactory,
                           IOptions<InformaticsGatewayConfiguration> configuration)
        {
            if (serviceScopeFactory is null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            }

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _serviceScope = serviceScopeFactory.CreateScope();
            _logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>() ?? throw new ServiceNotFoundException(nameof(ILoggerFactory));
            _logger = _logginFactory.CreateLogger<MllpService>();
            var tcpListenerFactory = _serviceScope.ServiceProvider.GetService<ITcpListenerFactory>() ?? throw new ServiceNotFoundException(nameof(ITcpListenerFactory));
            _tcpListener = tcpListenerFactory.CreateTcpListener(System.Net.IPAddress.Any, _configuration.Value.Hl7.Port);
            _mllpClientFactory = _serviceScope.ServiceProvider.GetService<IMllpClientFactory>() ?? throw new ServiceNotFoundException(nameof(IMllpClientFactory));
            _activeTasks = new ConcurrentDictionary<Guid, IMllpClient>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(async () =>
            {
                _tcpListener.Start();
                await BackgroundProcessing(cancellationToken).ConfigureAwait(true);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceRunning(ServiceName);
            _logger.Hl7ListeningOnPort(_configuration.Value.Hl7.Port);

            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            _tcpListener.Stop();
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private async Task BackgroundProcessing(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    WaitUntilAvailable(_configuration.Value.Hl7.MaximumNumberOfConnections);
                    var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    _logger.ClientConnected();

                    var mllpClient = _mllpClientFactory.CreateClient(client, _configuration.Value.Hl7, _logginFactory.CreateLogger<MllpClient>());
                    _ = mllpClient.Start(OnDisconnect, cancellationToken);
                    _activeTasks.TryAdd(mllpClient.ClientId, mllpClient);
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    _logger.Hl7SocketException(ex.Message);
                    if (ex.ErrorCode == SOCKET_OPERATION_CANCELLED)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ServiceInvalidOrCancelled(ServiceName, ex);
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        private void OnDisconnect(IMllpClient client, MllpClientResult result)
        {
            _activeTasks.Remove(client.ClientId, out _);
        }

        private void WaitUntilAvailable(int maximumNumberOfConnections)
        {
            var count = 0;
            while (ActiveConnections >= maximumNumberOfConnections)
            {
                if (++count % 25 == 1)
                {
                    _logger.MaxedOutHl7Connections(maximumNumberOfConnections);
                }
                Thread.Sleep(100);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var client in _activeTasks.Values)
                    {
                        client.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
