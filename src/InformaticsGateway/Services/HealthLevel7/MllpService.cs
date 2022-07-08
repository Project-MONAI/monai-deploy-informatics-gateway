// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
#pragma warning disable S2223 // Non-constant static fields should not be visible
        public static int ActiveConnections = 0;
#pragma warning restore S2223 // Non-constant static fields should not be visible

        private bool _disposedValue;
        private static readonly object SyncRoot = new();
        private readonly TcpListener _tcpListener;
        private readonly IServiceScope _serviceScope;
        private readonly ILoggerFactory _logginFactory;
        private readonly ILogger<MllpService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IDictionary<DateTimeOffset, Task> _activeTasks;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public string ServiceName => "HL7 Service";

        public MllpService(IServiceScopeFactory serviceScopeFactory,
                                IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScope = serviceScopeFactory.CreateScope();
            _logginFactory = _serviceScope.ServiceProvider.GetService<ILoggerFactory>();
            _logger = _logginFactory.CreateLogger<MllpService>();
            _configuration = configuration ?? throw new ServiceNotFoundException(nameof(configuration));
            _tcpListener = new TcpListener(System.Net.IPAddress.Any, _configuration.Value.Hl7.Port);
            _activeTasks = new Dictionary<DateTimeOffset, Task>();
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
                WaitUntilAvailable();

                var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _logger.ClientConnected();
                var mllpClient = new MllpClient(client, _configuration.Value.Hl7, _logginFactory.CreateLogger<MllpClient>());
                var task = mllpClient.Start(OnDisconnect, cancellationToken);

                _activeTasks.Add(DateTimeOffset.UtcNow, task);
            }
        }

        private void OnDisconnect(TcpClient client, MllpClientResult result)
        {
        }

        private void WaitUntilAvailable()
        {
            lock (SyncRoot)
            {
                while (ActiveConnections > _configuration.Value.Hl7.MaximumNumberOfConnections)
                {
                    Thread.Sleep(100);
                }
                ++ActiveConnections;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~HL7Service()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
