/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using FellowOakDicom.Network.Client.EventArguments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class ScpServiceTest
    {
        private static int s_nextPort = 11100;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IApplicationEntityManager> _associationDataProvider;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger<ScpService>> _logger;
        private readonly Mock<IHostApplicationLifetime> _appLifetime;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ScpServiceTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _associationDataProvider = new Mock<IApplicationEntityManager>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<ScpService>>();
            _appLifetime = new Mock<IHostApplicationLifetime>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _cancellationTokenSource = new CancellationTokenSource();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(ILoggerFactory)))
                .Returns(_loggerFactory.Object);
            _serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _associationDataProvider.Setup(p => p.Configuration).Returns(_configuration);
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "StartAsync - shall stop application if failed to start SCP listner")]
        public void StartAsync_ShallStopApplicationIfListnerFailedToStart()
        {
            _configuration.Value.Dicom.Scp.Port = -1;

            var service = new ScpService(_serviceScopeFactory.Object, _associationDataProvider.Object, _appLifetime.Object, _configuration);
            var task = service.StartAsync(_cancellationTokenSource.Token);
            task.Wait(1000);

            Assert.Equal(ServiceStatus.Cancelled, service.Status);
            _appLifetime.Verify(p => p.StopApplication(), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "StopAsync - shall be able to stop SCP listener")]
        public async Task StopAsync_ShallBeAbleToStopListener()
        {
            var service = await CreateService();

            _appLifetime.Verify(p => p.StopApplication(), Times.Never());

            await service.StopAsync(_cancellationTokenSource.Token);
            Assert.Equal(ServiceStatus.Stopped, service.Status);
        }

        [RetryFact(5, 250, DisplayName = "C-ECHO - Shall reject request if disabled")]
        public async Task CEcho_ShallRejectCEchoRequests()
        {
            _configuration.Value.Dicom.Scp.EnableVerification = false;
            _configuration.Value.Dicom.Scp.RejectUnknownSources = true;

            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);

            var countdownEvent = new CountdownEvent(1);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            await client.AddRequestAsync(new DicomCEchoRequest());
            client.AssociationRejected += (object sender, AssociationRejectedEventArgs e) =>
            {
                countdownEvent.Signal();
            };

            var exception = await Assert.ThrowsAsync<DicomAssociationRejectedException>(async () => await client.SendAsync());

            Assert.Equal(DicomRejectReason.ApplicationContextNotSupported, exception.RejectReason);
            Assert.Equal(DicomRejectSource.ServiceUser, exception.RejectSource);
            Assert.Equal(DicomRejectResult.Permanent, exception.RejectResult);

            _logger.VerifyLogging($"Verification service is disabled: rejecting association.", LogLevel.Warning, Times.Once());

            Assert.True(countdownEvent.Wait(1000));
        }

        [RetryFact(5, 250, DisplayName = "C-ECHO - Shall reject unknown calling AET")]
        public async Task CEcho_ShallRejecUnknownCallingAET()
        {
            _configuration.Value.Dicom.Scp.EnableVerification = true;
            _configuration.Value.Dicom.Scp.RejectUnknownSources = true;

            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);

            var countdownEvent = new CountdownEvent(1);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            await client.AddRequestAsync(new DicomCEchoRequest());
            client.AssociationRejected += (object sender, AssociationRejectedEventArgs e) =>
            {
                countdownEvent.Signal();
            };

            var exception = await Assert.ThrowsAsync<DicomAssociationRejectedException>(async () => await client.SendAsync());

            Assert.Equal(DicomRejectReason.CallingAENotRecognized, exception.RejectReason);
            Assert.Equal(DicomRejectSource.ServiceUser, exception.RejectSource);
            Assert.Equal(DicomRejectResult.Permanent, exception.RejectResult);

            Assert.True(countdownEvent.Wait(1000));
        }

        [RetryFact(5, 250, DisplayName = "C-ECHO - Shall reject unknown called AET")]
        public async Task CEcho_ShallRejecUnknownCalledAET()
        {
            _configuration.Value.Dicom.Scp.EnableVerification = true;
            _configuration.Value.Dicom.Scp.RejectUnknownSources = true;

            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(false);

            var countdownEvent = new CountdownEvent(1);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            await client.AddRequestAsync(new DicomCEchoRequest());
            client.AssociationRejected += (object sender, AssociationRejectedEventArgs e) =>
            {
                countdownEvent.Signal();
            };

            var exception = await Assert.ThrowsAsync<DicomAssociationRejectedException>(async () => await client.SendAsync());

            Assert.Equal(DicomRejectReason.CalledAENotRecognized, exception.RejectReason);
            Assert.Equal(DicomRejectSource.ServiceUser, exception.RejectSource);
            Assert.Equal(DicomRejectResult.Permanent, exception.RejectResult);

            Assert.True(countdownEvent.Wait(1000));
        }

        [RetryFact(5, 250, DisplayName = "C-ECHO - Shall accept")]
        public async Task CEcho_ShallAccept()
        {
            _configuration.Value.Dicom.Scp.EnableVerification = true;
            _configuration.Value.Dicom.Scp.RejectUnknownSources = true;

            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);

            var countdownEvent = new CountdownEvent(1);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            await client.AddRequestAsync(new DicomCEchoRequest());
            client.AssociationAccepted += (object sender, AssociationAcceptedEventArgs e) =>
            {
                countdownEvent.Signal();
            };

            await client.SendAsync();
            Assert.True(countdownEvent.Wait(1000));
        }

        [RetryFact(5, 250, DisplayName = "C-STORE - Shall reject when storage is low")]
        public async Task CStore_ShallRejecOnLowStorageSpace()
        {
            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.CanStore).Returns(false);

            var countdownEvent = new CountdownEvent(1);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            await client.AddRequestAsync(new DicomCStoreRequest(InstanceGenerator.GenerateDicomFile()));
            client.AssociationRejected += (object sender, AssociationRejectedEventArgs e) =>
            {
                countdownEvent.Signal();
            };

            var exception = await Assert.ThrowsAsync<DicomAssociationRejectedException>(async () => await client.SendAsync());

            Assert.Equal(DicomRejectReason.NoReasonGiven, exception.RejectReason);
            Assert.Equal(DicomRejectSource.ServiceUser, exception.RejectSource);
            Assert.Equal(DicomRejectResult.Permanent, exception.RejectResult);

            Assert.True(countdownEvent.Wait(2000));
        }

        [RetryFact(5, 250, DisplayName = "C-STORE - OnCStoreRequest - InsufficientStorageAvailableException")]
        public async Task CStore_OnCStoreRequest_InsufficientStorageAvailableException()
        {
            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.CanStore).Returns(true);
            _associationDataProvider.Setup(p => p.HandleCStoreRequest(It.IsAny<DicomCStoreRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>())).Throws(new InsufficientStorageAvailableException());

            var countdownEvent = new CountdownEvent(3);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            var request = new DicomCStoreRequest(InstanceGenerator.GenerateDicomFile());
            await client.AddRequestAsync(request);
            client.AssociationAccepted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            client.AssociationReleased += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            request.OnResponseReceived += (DicomCStoreRequest request, DicomCStoreResponse response) =>
            {
                Assert.Equal(DicomStatus.ResourceLimitation, response.Status);
                countdownEvent.Signal();
            };

            await client.SendAsync();
            Assert.True(countdownEvent.Wait(2000));
        }

        [RetryFact(5, 250, DisplayName = "C-STORE - OnCStoreRequest - IOException")]
        public async Task CStore_OnCStoreRequest_IoException()
        {
            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.CanStore).Returns(true);
            _associationDataProvider.Setup(p => p.HandleCStoreRequest(It.IsAny<DicomCStoreRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>())).Throws(new IOException { HResult = Constants.ERROR_HANDLE_DISK_FULL });
            var countdownEvent = new CountdownEvent(3);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            var request = new DicomCStoreRequest(InstanceGenerator.GenerateDicomFile());
            await client.AddRequestAsync(request);
            client.AssociationAccepted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            client.AssociationReleased += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            request.OnResponseReceived += (DicomCStoreRequest request, DicomCStoreResponse response) =>
            {
                Assert.Equal(DicomStatus.StorageStorageOutOfResources, response.Status);
                countdownEvent.Signal();
            };

            await client.SendAsync();
            Assert.True(countdownEvent.Wait(2000));
        }

        [RetryFact(5, 250, DisplayName = "C-STORE - OnCStoreRequest - Exception")]
        public async Task CStore_OnCStoreRequest_Exception()
        {
            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.CanStore).Returns(true);
            _associationDataProvider.Setup(p => p.HandleCStoreRequest(It.IsAny<DicomCStoreRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>())).Throws(new Exception());

            var countdownEvent = new CountdownEvent(3);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            var request = new DicomCStoreRequest(InstanceGenerator.GenerateDicomFile());
            await client.AddRequestAsync(request);
            client.AssociationAccepted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            client.AssociationReleased += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            request.OnResponseReceived += (DicomCStoreRequest request, DicomCStoreResponse response) =>
            {
                Assert.Equal(DicomStatus.ProcessingFailure, response.Status);
                countdownEvent.Signal();
            };

            await client.SendAsync();
            Assert.True(countdownEvent.Wait(2000));
        }

        [RetryFact(5, 250, DisplayName = "C-STORE - OnCStoreRequest - Success")]
        public async Task CStore_OnCStoreRequest_Success()
        {
            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.CanStore).Returns(true);
            _associationDataProvider.Setup(p => p.HandleCStoreRequest(It.IsAny<DicomCStoreRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()));

            var countdownEvent = new CountdownEvent(3);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            var request = new DicomCStoreRequest(InstanceGenerator.GenerateDicomFile());
            await client.AddRequestAsync(request);
            client.AssociationAccepted += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            client.AssociationReleased += (sender, e) =>
            {
                countdownEvent.Signal();
            };
            request.OnResponseReceived += (DicomCStoreRequest request, DicomCStoreResponse response) =>
            {
                Assert.Equal(DicomStatus.Success, response.Status);
                countdownEvent.Signal();
            };

            await client.SendAsync();
            Assert.True(countdownEvent.Wait(2000));
        }

        [RetryFact(5, 250, DisplayName = "C-STORE - Simulate client abort")]
        public async Task CStore_OnClientAbort()
        {
            _associationDataProvider.Setup(p => p.IsValidSourceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.IsAeTitleConfiguredAsync(It.IsAny<string>())).ReturnsAsync(true);
            _associationDataProvider.Setup(p => p.CanStore).Returns(true);
            _associationDataProvider.Setup(p => p.HandleCStoreRequest(It.IsAny<DicomCStoreRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()));

            var countdownEvent = new CountdownEvent(1);
            var service = await CreateService();

            var client = DicomClientFactory.Create("localhost", _configuration.Value.Dicom.Scp.Port, false, "STORESCU", "STORESCP");
            var request = new DicomCStoreRequest(InstanceGenerator.GenerateDicomFile());
            await client.AddRequestAsync(request);
            client.AssociationAccepted += (sender, e) =>
            {
                _cancellationTokenSource.Cancel();
                countdownEvent.Signal();
            };

            await client.SendAsync(_cancellationTokenSource.Token, DicomClientCancellationMode.ImmediatelyAbortAssociation);
            Assert.True(countdownEvent.Wait(2000));
            _logger.VerifyLogging($"Aborted {DicomAbortSource.ServiceUser} with reason {DicomAbortReason.NotSpecified}.", LogLevel.Warning, Times.Once());
        }

        private async Task<ScpService> CreateService()
        {
            var tryCount = 0;
            ScpService service = null;

            do
            {
                _configuration.Value.Dicom.Scp.Port = Interlocked.Increment(ref s_nextPort);
                if (service != null)
                {
                    service.Dispose();
                    await Task.Delay(100);
                }
                service = new ScpService(_serviceScopeFactory.Object, _associationDataProvider.Object, _appLifetime.Object, _configuration);
                _ = service.StartAsync(_cancellationTokenSource.Token);
            } while (service.Status != ServiceStatus.Running && tryCount++ < 5);

            Assert.Equal(ServiceStatus.Running, service.Status);
            return service;
        }
    }
}
