/*
 * Copyright 2022-2023 MONAI Consortium
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Api.Mllp
{
    internal sealed class MllpServiceHost : IHostedService, IDisposable, IMonaiService
    {
        private const int SOCKET_OPERATION_CANCELLED = 125;
        private bool _disposedValue;
        private readonly ITcpListener _tcpListener;
        private readonly IMllpClientFactory _mllpClientFactory;
        private readonly IObjectUploadQueue _uploadQueue;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IFileSystem _fileSystem;
        private readonly ILoggerFactory _logginFactory;
        private readonly ILogger<MllpServiceHost> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly ConcurrentDictionary<Guid, IMllpClient> _activeTasks;
        private readonly IMllpExtract _mIIpExtract;
        private readonly IInputHL7DataPlugInEngine _inputHL7DataPlugInEngine;
        private readonly IHl7ApplicationConfigRepository _hl7ApplicationConfigRepository;
        private DateTime _lastConfigRead = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public int ActiveConnections
        {
            get
            {
                return _activeTasks.Count;
            }
        }

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public string ServiceName => "HL7 Service";

        public MllpServiceHost(IServiceScopeFactory serviceScopeFactory, IOptions<InformaticsGatewayConfiguration> configuration)
        {
            ArgumentNullException.ThrowIfNull(serviceScopeFactory, nameof(serviceScopeFactory));

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var serviceScope = serviceScopeFactory.CreateScope();
            _logginFactory = serviceScope.ServiceProvider.GetService<ILoggerFactory>() ?? throw new ServiceNotFoundException(nameof(ILoggerFactory));
            _logger = _logginFactory.CreateLogger<MllpServiceHost>();
            var tcpListenerFactory = serviceScope.ServiceProvider.GetService<ITcpListenerFactory>() ?? throw new ServiceNotFoundException(nameof(ITcpListenerFactory));
            _tcpListener = tcpListenerFactory.CreateTcpListener(System.Net.IPAddress.Any, _configuration.Value.Hl7.Port);
            _mllpClientFactory = serviceScope.ServiceProvider.GetService<IMllpClientFactory>() ?? throw new ServiceNotFoundException(nameof(IMllpClientFactory));
            _uploadQueue = serviceScope.ServiceProvider.GetService<IObjectUploadQueue>() ?? throw new ServiceNotFoundException(nameof(IObjectUploadQueue));
            _payloadAssembler = serviceScope.ServiceProvider.GetService<IPayloadAssembler>() ?? throw new ServiceNotFoundException(nameof(IPayloadAssembler));
            _fileSystem = serviceScope.ServiceProvider.GetService<IFileSystem>() ?? throw new ServiceNotFoundException(nameof(IFileSystem));
            _storageInfoProvider = serviceScope.ServiceProvider.GetService<IStorageInfoProvider>() ?? throw new ServiceNotFoundException(nameof(IStorageInfoProvider));
            _mIIpExtract = serviceScope.ServiceProvider.GetService<IMllpExtract>() ?? throw new ServiceNotFoundException(nameof(IMllpExtract));
            _activeTasks = new ConcurrentDictionary<Guid, IMllpClient>();
            _inputHL7DataPlugInEngine = serviceScope.ServiceProvider.GetService<IInputHL7DataPlugInEngine>() ?? throw new ServiceNotFoundException(nameof(IInputHL7DataPlugInEngine));
            _hl7ApplicationConfigRepository = serviceScope.ServiceProvider.GetService<IHl7ApplicationConfigRepository>() ?? throw new ServiceNotFoundException(nameof(IHl7ApplicationConfigRepository));
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
                IMllpClient? mllpClient = null;
                try
                {
                    WaitUntilAvailable(_configuration.Value.Hl7.MaximumNumberOfConnections);
                    var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    _logger.ClientConnected();

                    if (!_storageInfoProvider.HasSpaceAvailableToStore)
                    {
                        _logger.Hl7DisconnectedDueToLowStorageSpace(_storageInfoProvider.AvailableFreeSpace);
                        client.Close();
                        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    mllpClient = _mllpClientFactory.CreateClient(client, _configuration.Value.Hl7, _logginFactory.CreateLogger<MllpClient>());
                    _ = mllpClient.Start(OnDisconnect, cancellationToken);
                    _activeTasks.TryAdd(mllpClient.ClientId, mllpClient);
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    _logger.Hl7SocketException(ex.Message);

                    if (mllpClient is not null)
                    {
                        mllpClient.Dispose();
                        _activeTasks.Remove(mllpClient.ClientId, out _);
                    }

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

        private async Task OnDisconnect(IMllpClient client, MllpClientResult result)
        {
            Guard.Against.Null(client, nameof(client));
            Guard.Against.Null(result, nameof(result));

            await ConfigurePlugInEngine().ConfigureAwait(false);

            try
            {
                foreach (var message in result.Messages)
                {
                    var newMessage = message;
                    var hl7Filemetadata = new Hl7FileStorageMetadata(client.ClientId.ToString(), DataService.HL7, client.ClientIp);
                    var configItem = await _mIIpExtract.GetConfigItem(message).ConfigureAwait(false);
                    if (configItem is not null)
                    {
                        await _inputHL7DataPlugInEngine.ExecutePlugInsAsync(message, hl7Filemetadata, configItem).ConfigureAwait(false);
                        newMessage = await _mIIpExtract.ExtractInfo(hl7Filemetadata, message, configItem).ConfigureAwait(false);

                        _logger.HL7MessageAfterPluginProcessing(newMessage.HL7Message, hl7Filemetadata.CorrelationId);
                    }
                    _logger.Hl7MessageReceieved(newMessage.HL7Message);
                    await hl7Filemetadata.SetDataStream(newMessage.HL7Message, _configuration.Value.Storage.TemporaryDataStorage, _fileSystem, _configuration.Value.Storage.LocalTemporaryStoragePath).ConfigureAwait(false);
                    var payloadId = await _payloadAssembler.Queue(client.ClientId.ToString(), hl7Filemetadata, new DataOrigin { DataService = DataService.HL7, Source = client.ClientIp, Destination = FileStorageMetadata.IpAddress() }).ConfigureAwait(false);
                    hl7Filemetadata.PayloadId ??= payloadId.ToString();
                    _uploadQueue.Queue(hl7Filemetadata);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorHandlingHl7Results(ex);
            }
            finally
            {
                _activeTasks.Remove(client.ClientId, out _);
                _logger.Hl7ClientRemoved(client.ClientId);
                client.Dispose();
            }
        }

        private async Task ConfigurePlugInEngine()
        {
            var configs = await _hl7ApplicationConfigRepository.GetAllAsync().ConfigureAwait(false);
            if (configs is not null && configs.Count is not 0 && configs.Max(c => c.LastModified) > _lastConfigRead)
            {
                var pluginAssemblies = new List<string>();
                foreach (var config in configs.Where(p => p.PlugInAssemblies?.Count > 0))
                {
                    try
                    {
                        pluginAssemblies.AddRange(config.PlugInAssemblies.Where(p => string.IsNullOrWhiteSpace(p) is false && pluginAssemblies.Contains(p) is false));
                    }
                    catch (Exception ex)
                    {
                        _logger.HL7PluginLoadingExceptions(ex);
                    }
                }
                if (pluginAssemblies.Count is not 0)
                {
                    _inputHL7DataPlugInEngine.Configure(pluginAssemblies);
                }
            }
            _lastConfigRead = DateTime.UtcNow;
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
