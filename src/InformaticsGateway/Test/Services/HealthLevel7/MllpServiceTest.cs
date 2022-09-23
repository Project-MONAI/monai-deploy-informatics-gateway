/*
 * Copyright 2022 MONAI Consortium
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

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
        private readonly Mock<ILogger<MllpService>> _logger;
        private readonly IServiceProvider _serviceProvider;

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

            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScope = new Mock<IServiceScope>();
            _logger = new Mock<ILogger<MllpService>>();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);

            var services = new ServiceCollection();
            services.AddScoped(p => _loggerFactory.Object);
            services.AddScoped(p => _tcpListenerFactory.Object);
            services.AddScoped(p => _mllpClientFactory.Object);
            services.AddScoped(p => _uploadQueue.Object);
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _fileSystem.Object);
            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _tcpListenerFactory.Setup(p => p.CreateTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(_tcpListener.Object);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact()]
        public void GivenAMllpService_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new MllpService(null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpService(_serviceScopeFactory.Object, null));

            new MllpService(_serviceScopeFactory.Object, _options);
        }

        [RetryFact()]
        public void GivenAMllpService_WhenStartAsyncIsCalled_ExpectServiceStartupNormally()
        {
            var service = new MllpService(_serviceScopeFactory.Object, _options);
            var task = service.StartAsync(_cancellationTokenSource.Token);

            Assert.NotNull(task);
            Assert.Equal(ServiceStatus.Running, service.Status);
        }

        [RetryFact()]
        public void GivenAMllpService_WhenStopAsyncIsCalled_ExpectServiceStopsNormally()
        {
            _tcpListener.Setup(p => p.Stop());
            var service = new MllpService(_serviceScopeFactory.Object, _options);
            var task = service.StopAsync(_cancellationTokenSource.Token);

            Assert.NotNull(task);
            Assert.Equal(ServiceStatus.Stopped, service.Status);
            _tcpListener.Verify(p => p.Stop(), Times.Once());
        }

        [RetryFact(10, 100)]
        public void GivenTcpConnections_WhenConnectsAndDisconnectsFromMllpService_ExpectItToTrackActiveConnections()
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
                        Thread.Sleep(100);
                    }
                });

            var service = new MllpService(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(2000));
            Thread.Sleep(200);
            Assert.Equal(checkEvent.InitialCount, service.ActiveConnections);

            foreach (var action in actions.Keys)
            {
                actions[action](action, new MllpClientResult(null, null));
            }
            Assert.Equal(0, service.ActiveConnections);
        }

        [RetryFact]
        public void GivenAMllpService_WhenMaximumConnectionLimitIsConfigure_ExpectTheServiceToAbideByTheLimit()
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

            var service = new MllpService(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            checkEvent.Wait();
            Thread.Sleep(500);
            Assert.Equal(_options.Value.Hl7.MaximumNumberOfConnections, service.ActiveConnections);
            _tcpListener.Verify(p => p.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.Exactly(_options.Value.Hl7.MaximumNumberOfConnections));

            _logger.VerifyLoggingMessageBeginsWith($"Maximum number {_options.Value.Hl7.MaximumNumberOfConnections} of clients reached.", LogLevel.Information, Times.AtLeastOnce());
        }

        [RetryFact]
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

            var service = new MllpService(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(2000));
            await Task.Delay(200).ConfigureAwait(false);
            Assert.True(service.ActiveConnections > 0);

            _cancellationTokenSource.Cancel();
            await Task.Delay(500).ConfigureAwait(false);

            service.Dispose();
            client.Verify(p => p.Dispose(), Times.Exactly(callCount));
        }

        [RetryFact]
        public async Task GivenATcpClientWithHl7Messages_WhenDisconnected_ExpectMessageToBeQueued()
        {
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();
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

            var service = new MllpService(_serviceScopeFactory.Object, _options);
            _ = service.StartAsync(_cancellationTokenSource.Token);

            Assert.True(checkEvent.Wait(2000));
            await Task.Delay(500).ConfigureAwait(false);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Exactly(3));
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>()), Times.Exactly(3));
        }
    }
}
