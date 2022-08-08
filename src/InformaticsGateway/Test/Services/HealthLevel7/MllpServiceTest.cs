// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.HealthLevel7
{
    public class MllpServiceTest
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Mock<ITcpListenerFactory> _tcpListenerFactory;
        private readonly Mock<IMllpClientFactory> _mllpClientFactory;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger<MllpService>> _logger;
        private readonly Mock<ITcpListener> _tcpListener;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        public MllpServiceTest()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _tcpListenerFactory = new Mock<ITcpListenerFactory>();
            _mllpClientFactory = new Mock<IMllpClientFactory>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<MllpService>>();
            _tcpListener = new Mock<ITcpListener>();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(ILoggerFactory)))
                .Returns(_loggerFactory.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(ITcpListenerFactory)))
                .Returns(_tcpListenerFactory.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(IMllpClientFactory)))
                .Returns(_mllpClientFactory.Object);

            _serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);

            _options = Options.Create(new InformaticsGatewayConfiguration());

            _tcpListenerFactory.Setup(p => p.CreateTcpListener(It.IsAny<IPAddress>(), It.IsAny<int>())).Returns(_tcpListener.Object);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(DisplayName = "Constructor")]
        public void Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new MllpService(null, null));
            Assert.Throws<ArgumentNullException>(() => new MllpService(_serviceScopeFactory.Object, null));

            new MllpService(_serviceScopeFactory.Object, _options);
        }

        [RetryFact(DisplayName = "Can start service")]
        public void CanStart()
        {
            var service = new MllpService(_serviceScopeFactory.Object, _options);
            var task = service.StartAsync(_cancellationTokenSource.Token);

            Assert.NotNull(task);
            Assert.Equal(ServiceStatus.Running, service.Status);
        }

        [RetryFact(DisplayName = "Can stop service")]
        public void CanStop()
        {
            _tcpListener.Setup(p => p.Stop());
            var service = new MllpService(_serviceScopeFactory.Object, _options);
            var task = service.StopAsync(_cancellationTokenSource.Token);

            Assert.NotNull(task);
            Assert.Equal(ServiceStatus.Stopped, service.Status);
            _tcpListener.Verify(p => p.Stop(), Times.Once());
        }

        [RetryFact(10, 100, DisplayName = "Tracks active connections")]
        public void TracksActiveConnections()
        {
            var actions = new Dictionary<IMllpClient, Action<IMllpClient, MllpClientResult>>();
            var mllpClients = new List<Mock<IMllpClient>>();
            var checkEvent = new CountdownEvent(5);
            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    var client = new Mock<IMllpClient>();
                    client.Setup(p => p.Start(It.IsAny<Action<IMllpClient, MllpClientResult>>(), It.IsAny<CancellationToken>()))
                        .Callback<Action<IMllpClient, MllpClientResult>, CancellationToken>((action, cancellationToken) =>
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
                actions[action](action, null);
            }
            Assert.Equal(0, service.ActiveConnections);
        }

        [RetryFact(DisplayName = "Abides by the maximum connection limit")]
        public void AbidesByMaximumConnectionLimit()
        {
            var checkEvent = new CountdownEvent(_options.Value.Hl7.MaximumNumberOfConnections);
            var mllpClients = new List<Mock<IMllpClient>>();
            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
               .Returns(() =>
               {
                   var client = new Mock<IMllpClient>();
                   client.Setup(p => p.Start(It.IsAny<Action<IMllpClient, MllpClientResult>>(), It.IsAny<CancellationToken>()))
                       .Callback<Action<IMllpClient, MllpClientResult>, CancellationToken>((action, cancellationToken) =>
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

        [RetryFact(DisplayName = "Dispose clients")]
        public async Task DisposeClients()
        {
            var checkEvent = new ManualResetEventSlim();
            var client = new Mock<IMllpClient>();
            var callCount = 0;
            _mllpClientFactory.Setup(p => p.CreateClient(It.IsAny<ITcpClientAdapter>(), It.IsAny<Hl7Configuration>(), It.IsAny<ILogger<MllpClient>>()))
                .Returns(() =>
                {
                    client.Setup(p => p.Start(It.IsAny<Action<IMllpClient, MllpClientResult>>(), It.IsAny<CancellationToken>()))
                        .Callback<Action<IMllpClient, MllpClientResult>, CancellationToken>((action, cancellationToken) =>
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
    }
}
