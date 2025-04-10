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
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HL7.Dotnetcore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Api.Mllp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.Messaging.Events;
using Moq;
using xRetry;
using Xunit;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;

namespace Monai.Deploy.InformaticsGateway.Test.Services.HealthLevel7
{
    public class MllpServiceTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<ITcpListenerFactory> _tcpListenerFactory;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<IMllpClientFactory> _mllpClientFactory;
        private readonly Mock<IObjectUploadQueue> _uploadQueue;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<ITcpListener> _tcpListener;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<ILogger<MllpServiceHost>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly Mock<IMllpExtract> _mIIpExtract = new Mock<IMllpExtract>();
        private readonly Mock<IInputHL7DataPlugInEngine> _hl7DataPlugInEngine = new Mock<IInputHL7DataPlugInEngine>();
        private readonly Mock<IHl7ApplicationConfigRepository> _hl7ApplicationConfigRepository = new Mock<IHl7ApplicationConfigRepository>();

        public MllpServiceTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _tcpListenerFactory = new Mock<ITcpListenerFactory>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _mllpClientFactory = new Mock<IMllpClientFactory>();
            _uploadQueue = new Mock<IObjectUploadQueue>();
            _payloadAssembler = new Mock<IPayloadAssembler>();
            _tcpListener = new Mock<ITcpListener>();
            _fileSystem = new Mock<IFileSystem>();
            _storageInfoProvider = new Mock<IStorageInfoProvider>();

            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScope = new Mock<IServiceScope>();
            _logger = new Mock<ILogger<MllpServiceHost>>();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);

            var services = new ServiceCollection();
            services.AddScoped(p => _loggerFactory.Object);
            services.AddScoped(p => _tcpListenerFactory.Object);
            services.AddScoped(p => _mllpClientFactory.Object);
            services.AddScoped(p => _uploadQueue.Object);
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _fileSystem.Object);
            services.AddScoped(p => _storageInfoProvider.Object);
            services.AddScoped(p => _mIIpExtract.Object);
            services.AddScoped(p => _hl7DataPlugInEngine.Object);
            services.AddScoped(p => _hl7ApplicationConfigRepository.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _tcpListenerFactory.Setup(p => p.CreateTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(_tcpListener.Object);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(true);
            _options.Value.Storage.TemporaryDataStorage = TemporaryDataStorageLocation.Memory;
        }

        [RetryFact(10, 250)]
        public void GivenAMllpService_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new MllpServiceHost(null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpServiceHost(_serviceScopeFactory.Object, null));

            new MllpServiceHost(_serviceScopeFactory.Object, _options);
        }

        [RetryFact(5, 250)]
        public void GivenAMllpService_WhenStartAsyncIsCalled_ExpectServiceStartupNormally()
        {
            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            var task = service.StartAsync(_cancellationTokenSource.Token);

            Assert.NotNull(task);
            Assert.Equal(ServiceStatus.Running, service.Status);
        }

        [RetryFact(10, 250)]
        public void GivenAMllpService_WhenStopAsyncIsCalled_ExpectServiceStopsNormally()
        {
            _tcpListener.Setup(p => p.Stop());
            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            var task = service.StopAsync(_cancellationTokenSource.Token);

            Assert.NotNull(task);
            Assert.Equal(ServiceStatus.Stopped, service.Status);
            _tcpListener.Verify(p => p.Stop(), Times.Once());
        }

        [RetryFact(10, 100)]
        public async Task GivenTcpConnections_WhenConnectsAndDisconnectsFromMllpService_ExpectItToTrackActiveConnections()
        {
            var actions = new Dictionary<IMllpClient, Func<IMllpClient, MllpClientResult, Task>>();
            var mllpClients = new List<Mock<IMllpClient>>();
            var checkEvent = new CountdownEvent(5);
            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    var client = new Mock<IMllpClient>();
                    client.Setup(p => p.Start(It.IsAny<Func<IMllpClient, MllpClientResult, Task>>(), It.IsAny<CancellationToken>()))
                        .Callback<Func<IMllpClient, MllpClientResult, Task>, CancellationToken>((action, cancellationToken) =>
                        {
                            actions.Add(client.Object, action);
                            checkEvent.Signal();
                        });
                    client.SetupGet(p => p.ClientId).Returns(Guid.NewGuid());
                    mllpClients.Add(client);
                    return client.Object;
                });

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (checkEvent.CurrentCount > 0)
                    {
                        return ValueTask.FromResult((new Mock<ITcpClientAdapter>()).Object);
                    }

                    while (true)
                    {
                        Task.Delay(100).GetAwaiter().GetResult();
                    }
                });

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(3000));
            await Task.Delay(200);
            Assert.Equal(checkEvent.InitialCount, service.ActiveConnections);

            foreach (var action in actions.Keys)
            {
                await actions[action](action, new MllpClientResult(null, null));
            }
            Assert.Equal(0, service.ActiveConnections);
        }

        [RetryFact(10, 250)]
        public async Task GivenAMllpService_WhenMaximumConnectionLimitIsConfigure_ExpectTheServiceToAbideByTheLimit()
        {
            var checkEvent = new CountdownEvent(_options.Value.Hl7.MaximumNumberOfConnections);
            var mllpClients = new List<Mock<IMllpClient>>();
            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
               .Returns(() =>
               {
                   var client = new Mock<IMllpClient>();
                   client.Setup(p => p.Start(It.IsAny<Func<IMllpClient, MllpClientResult, Task>>(), It.IsAny<CancellationToken>()))
                       .Callback<Func<IMllpClient, MllpClientResult, Task>, CancellationToken>((action, cancellationToken) =>
                       {
                           checkEvent.Signal();
                       });
                   client.SetupGet(p => p.ClientId).Returns(Guid.NewGuid());
                   mllpClients.Add(client);
                   return client.Object;
               });

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult((new Mock<ITcpClientAdapter>()).Object));

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            checkEvent.Wait();
            await Task.Delay(500);
            Assert.Equal(_options.Value.Hl7.MaximumNumberOfConnections, service.ActiveConnections);
            _tcpListener.Verify(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.Exactly(_options.Value.Hl7.MaximumNumberOfConnections));

            _logger.VerifyLoggingMessageBeginsWith($"Maximum number {_options.Value.Hl7.MaximumNumberOfConnections} of clients reached.", LogLevel.Information, Times.AtLeastOnce());
        }

        [RetryFact(10, 250)]
        public async Task GivenConnectedTcpClients_WhenDisconnects_ExpectServiceToDisposeResources()
        {
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();
            var callCount = 0;
            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    client.Setup(p => p.Start(It.IsAny<Func<IMllpClient, MllpClientResult, Task>>(), It.IsAny<CancellationToken>()))
                        .Callback<Func<IMllpClient, MllpClientResult, Task>, CancellationToken>((action, cancellationToken) =>
                        {
                            callCount++;
                            checkEvent.Set();
                        });
                    client.Setup(p => p.Dispose());
                    client.SetupGet(p => p.ClientId).Returns(Guid.NewGuid());
                    return client.Object;
                });

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult((new Mock<ITcpClientAdapter>()).Object));

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(3000));
            await Task.Delay(200).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            Assert.True(service.ActiveConnections > 0);

            _cancellationTokenSource.Cancel();
            await Task.Delay(500).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            service.Dispose();
            client.Verify(p => p.Dispose(), Times.Exactly(callCount));
        }

        [RetryFact(10, 250)]
        public async Task GivenATcpClientWithHl7Messages_WhenStorageSpaceIsLow_ExpectToDisconnect()
        {
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(false);
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();
            var clientAdapter = new Mock<ITcpClientAdapter>();

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult(clientAdapter.Object));

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            _cancellationTokenSource.CancelAfter(400);
            await Task.Delay(500).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            clientAdapter.Verify(p => p.Close(), Times.AtLeastOnce());
            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Never());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Never());
        }

        [RetryFact(10, 250)]
        public async Task GivenATcpClientWithHl7Messages_WhenDisconnected_ExpectMessageToBeQueued()
        {
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();
            _mIIpExtract.Setup(e => e.ExtractInfo(It.IsAny<Hl7FileStorageMetadata>(), It.IsAny<Message>(), It.IsAny<Hl7ApplicationConfigEntity>()))
                .ReturnsAsync((Hl7FileStorageMetadata meta, Message Msg, Hl7ApplicationConfigEntity configItem) => Msg);

            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    client.Setup(p => p.Start(It.IsAny<Func<IMllpClient, MllpClientResult, Task>>(), It.IsAny<CancellationToken>()))
                        .Callback<Func<IMllpClient, MllpClientResult, Task>, CancellationToken>((action, cancellationToken) =>
                        {
                            var results = new MllpClientResult(
                                 new List<HL7.Dotnetcore.Message>
                                 {
                                     new HL7.Dotnetcore.Message(""),
                                     new HL7.Dotnetcore.Message(""),
                                     new HL7.Dotnetcore.Message(""),
                                 }, null);
                            action(client.Object, results);
                            checkEvent.Set();
                            _cancellationTokenSource.Cancel();
                        });
                    client.Setup(p => p.Dispose());
                    client.SetupGet(p => p.ClientId).Returns(Guid.NewGuid());
                    return client.Object;
                });

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult((new Mock<ITcpClientAdapter>()).Object));

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(3000));
            await Task.Delay(500).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(3));
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<DataOrigin>()), Times.Exactly(3));
        }

        [RetryFact(10, 250)]
        public async Task GivenATcpClientWithHl7Messages_WhenDisconnected_ExpectMessageToBeRePopulated()
        {
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();

            _mIIpExtract.Setup(e => e.ExtractInfo(It.IsAny<Hl7FileStorageMetadata>(), It.IsAny<Message>(), It.IsAny<Hl7ApplicationConfigEntity>()))
                .ReturnsAsync((Hl7FileStorageMetadata meta, Message Msg, Hl7ApplicationConfigEntity configItem) => Msg);

            _mIIpExtract.Setup(e => e.GetConfigItem(It.IsAny<Message>()))
                .ReturnsAsync((Message Msg) => new Hl7ApplicationConfigEntity());

            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    client.Setup(p => p.Start(It.IsAny<Func<IMllpClient, MllpClientResult, Task>>(), It.IsAny<CancellationToken>()))
                        .Callback<Func<IMllpClient, MllpClientResult, Task>, CancellationToken>((action, cancellationToken) =>
                        {
                            var results = new MllpClientResult(
                                 new List<HL7.Dotnetcore.Message>
                                 {
                                     new HL7.Dotnetcore.Message(""),
                                     new HL7.Dotnetcore.Message(""),
                                     new HL7.Dotnetcore.Message(""),
                                 }, null);
                            action(client.Object, results);
                            checkEvent.Set();
                            _cancellationTokenSource.Cancel();
                        });
                    client.Setup(p => p.Dispose());
                    client.SetupGet(p => p.ClientId).Returns(Guid.NewGuid());
                    return client.Object;
                });

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult((new Mock<ITcpClientAdapter>()).Object));

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(3000));
            await Task.Delay(500).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);

            _mIIpExtract.Verify(p => p.ExtractInfo(It.IsAny<Hl7FileStorageMetadata>(), It.IsAny<Message>(), It.IsAny<Hl7ApplicationConfigEntity>()), Times.Exactly(3));
        }

        [RetryFact(10, 250)]
        public async Task GivenATcpClientWithHl7Messages_ShouldntAdddBlankPlugin()
        {
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();
            _mIIpExtract.Setup(e => e.ExtractInfo(It.IsAny<Hl7FileStorageMetadata>(), It.IsAny<Message>(), It.IsAny<Hl7ApplicationConfigEntity>()))
                .ReturnsAsync((Hl7FileStorageMetadata meta, Message Msg, Hl7ApplicationConfigEntity configItem) => Msg);

            _hl7ApplicationConfigRepository.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Hl7ApplicationConfigEntity> { new Hl7ApplicationConfigEntity {
                    PlugInAssemblies = [""]
                } });

            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    client.Setup(p => p.Start(It.IsAny<Func<IMllpClient, MllpClientResult, Task>>(), It.IsAny<CancellationToken>()))
                        .Callback<Func<IMllpClient, MllpClientResult, Task>, CancellationToken>((action, cancellationToken) =>
                        {
                            var results = new MllpClientResult(
                                 new List<HL7.Dotnetcore.Message>
                                 {
                                     new("")
                                 }, null);
                            action(client.Object, results);
                            checkEvent.Set();
                            _cancellationTokenSource.Cancel();
                        });
                    client.Setup(p => p.Dispose());
                    client.SetupGet(p => p.ClientId).Returns(Guid.NewGuid());
                    return client.Object;
                });

            _tcpListener.Setup(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult((new Mock<ITcpClientAdapter>()).Object));

            var service = new MllpServiceHost(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(3000));
            await Task.Delay(500).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            _hl7DataPlugInEngine.Verify(p => p.Configure(It.IsAny<IReadOnlyList<string>>()), Times.Never());
        }
    }
}