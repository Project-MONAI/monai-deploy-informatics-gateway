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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Scu;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scu
{
    [Collection("SCP Listener")]
    public class ScuServiceTest
    {
        private readonly DicomScpFixture _dicomScp;
        private readonly int _port = 1104;

        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<ILogger<ScuService>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<ILogger<ScuQueue>> _scuQueueLogger;
        private readonly IScuQueue _scuQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ServiceProvider _serviceProvider;
        private readonly Mock<IServiceScope> _serviceScope;

        public ScuServiceTest(DicomScpFixture dicomScp)
        {
            _dicomScp = dicomScp ?? throw new ArgumentNullException(nameof(dicomScp));

            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _scuQueueLogger = new Mock<ILogger<ScuQueue>>();
            _scuQueue = new ScuQueue(_scuQueueLogger.Object);
            _logger = new Mock<ILogger<ScuService>>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _cancellationTokenSource = new CancellationTokenSource();
            _serviceScope = new Mock<IServiceScope>();

            var services = new ServiceCollection();
            services.AddScoped(p => _scuQueue);
            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _dicomScp.Start(_port);
        }

        [RetryFact(10,200)]
        public void GivenAScuService_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new ScuService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ScuService(_serviceScopeFactory.Object, _logger.Object, null));

            _ = new ScuService(_serviceScopeFactory.Object, _logger.Object, _options);
        }

        [RetryFact(10,200)]
        public void GivenAScuService_WhenStartAsyncIsCalled_ExpectServiceStatusToBeSet()
        {
            var svc = new ScuService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);
        }

        [RetryFact(10,200)]
        public async Task GivenAValidDicomEntity_WhenRequestToCEcho_ExpectToReturnSucess()
        {
            var svc = new ScuService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var request = new ScuWorkRequest(Guid.NewGuid().ToString(), RequestType.CEcho, "localhost", _port, DicomScpFixture.s_aETITLE);

            var response = await _scuQueue.Queue(request, _cancellationTokenSource.Token);

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(ResponseError.None, response.Error);
            Assert.Empty(response.Message);
        }

        [RetryFact(10,200)]
        public async Task GivenACEchoRequest_WhenRejected_ReturnStatusAssociationRejected()
        {
            var svc = new ScuService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var request = new ScuWorkRequest(Guid.NewGuid().ToString(), RequestType.CEcho, "localhost", _port, "BADAET");

            var response = await _scuQueue.Queue(request, _cancellationTokenSource.Token);

            Assert.Equal(ResponseStatus.Failure, response.Status);
            Assert.Equal(ResponseError.AssociationRejected, response.Error);
            Assert.StartsWith("Association rejected", response.Message);
        }

        [RetryFact(10,200)]
        public async Task GivenACEchoRequest_WhenAborted_ReturnStatusAssociationAborted()
        {
            var svc = new ScuService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var request = new ScuWorkRequest(Guid.NewGuid().ToString(), RequestType.CEcho, "localhost", _port, "ABORT");

            var response = await _scuQueue.Queue(request, _cancellationTokenSource.Token);

            Assert.Equal(ResponseStatus.Failure, response.Status);
            Assert.Equal(ResponseError.AssociationAborted, response.Error);
            Assert.StartsWith("Association Abort", response.Message);
        }

        [RetryFact(10,200)]
        public async Task GivenACEchoRequest_WhenRemoteServerIsUnreachable_ReturnStatusAssociationRejected()
        {
            var svc = new ScuService(_serviceScopeFactory.Object, _logger.Object, _options);
            _ = svc.StartAsync(_cancellationTokenSource.Token);

            Assert.Equal(ServiceStatus.Running, svc.Status);

            var request = new ScuWorkRequest(Guid.NewGuid().ToString(), RequestType.CEcho, "UNKNOWNHOST123456789", _port, DicomScpFixture.s_aETITLE);

            var response = await _scuQueue.Queue(request, _cancellationTokenSource.Token);

            Assert.Equal(ResponseStatus.Failure, response.Status);
            Assert.Equal(ResponseError.Unhandled, response.Error);
            Assert.StartsWith("One or more error", response.Message);
        }
    }
}
