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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ControlServiceTest
    {
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly Mock<IContainerRunnerFactory> _containerRunnerFactory;
        private readonly Mock<IContainerRunner> _containerRunner;
        private readonly Mock<ILogger<ControlService>> _logger;

        public ControlServiceTest()
        {
            _configurationService = new Mock<IConfigurationService>();
            _containerRunnerFactory = new Mock<IContainerRunnerFactory>();
            _logger = new Mock<ILogger<ControlService>>();
            _containerRunner = new Mock<IContainerRunner>();

            _containerRunnerFactory.Setup(p => p.GetContainerRunner()).Returns(_containerRunner.Object);
        }

        [Fact(DisplayName = "ControlServiceTest constructor")]
        public void ControlServiceTest_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new ControlService(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ControlService(_containerRunnerFactory.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new ControlService(_containerRunnerFactory.Object, _logger.Object, null));
        }

        [Fact(DisplayName = "Start - throw exception wihtout any application images found")]
        public async Task Start_WithoutAnyApplicationInstalled()
        {
            _containerRunner.Setup(p => p.GetLatestApplicationVersion(CancellationToken.None)).ReturnsAsync(default(ImageVersion));
            _configurationService.SetupGet(p => p.Configurations.DockerImagePrefix).Returns("PREFIX");

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            var exception = await Assert.ThrowsAsync<ControlException>(async () => await service.Start());

            Assert.Equal(ExitCodes.Start_Error_ApplicationNotFound, exception.ErrorCode);
        }

        [Fact(DisplayName = "Start - throw exception when application image is running")]
        public async Task Start_ApplicationImageIsAlreadyRunning()
        {
            _containerRunner.Setup(p => p.GetLatestApplicationVersion(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ImageVersion { Id = Guid.NewGuid().ToString("N") });

            _containerRunner.Setup(p => p.IsApplicationRunning(It.IsAny<ImageVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunnerState { IsRunning = true, Id = Guid.NewGuid().ToString("N") });

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            var exception = await Assert.ThrowsAsync<ControlException>(async () => await service.Start());

            Assert.Equal(ExitCodes.Start_Error_ApplicationAlreadyRunning, exception.ErrorCode);
        }

        [Fact(DisplayName = "Start - starts the application")]
        public async Task Start_StartsTheLatestApplicationImage()
        {
            _containerRunner.Setup(p => p.GetLatestApplicationVersion(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new ImageVersion { Id = Guid.NewGuid().ToString("N") });

            _containerRunner.Setup(p => p.IsApplicationRunning(It.IsAny<ImageVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunnerState { IsRunning = false, Id = Guid.NewGuid().ToString("N") });

            _containerRunner.Setup(p => p.StartApplication(It.IsAny<ImageVersion>(), It.IsAny<CancellationToken>()));

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            await service.Start();
        }

        [Fact(DisplayName = "Stop - no running application")]
        public async Task Stop_NoRunningApplication()
        {
            _containerRunner.Setup(p => p.GetApplicationVersions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(List<ImageVersion>));

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            await service.Stop();
        }

        [Fact(DisplayName = "Stop - error stopping application")]
        public async Task Stop_ErrorStopingApplication()
        {
            var data = new List<ImageVersion>
                {
                    new ImageVersion{  Id = Guid.NewGuid().ToString("N"), Version = "1", Created =DateTime.UtcNow}
                };

            _containerRunner.Setup(p => p.GetApplicationVersions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(data);
            _containerRunner.Setup(p => p.IsApplicationRunning(It.IsAny<ImageVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunnerState { IsRunning = true, Id = data[0].Id });
            _containerRunner.Setup(p => p.StopApplication(It.IsAny<RunnerState>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _configurationService.Setup(p => p.Configurations.Runner).Returns(Runner.Docker);

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            await service.Stop();

            _logger.VerifyLogging($"Error may have occurred stopping {Strings.ApplicationName} with container ID {data[0].Id}. Please verify with the applicatio state with {Runner.Docker}.", LogLevel.Warning, Times.Once());
        }

        [Fact(DisplayName = "Stop - stops running applications")]
        public async Task Stop_StopRunningApplications()
        {
            var data = new List<ImageVersion>
                {
                    new ImageVersion{  Id = Guid.NewGuid().ToString("N"), Version = "1", Created =DateTime.UtcNow},
                    new ImageVersion{  Id = Guid.NewGuid().ToString("N"), Version = "2", Created =DateTime.UtcNow},
                    new ImageVersion{  Id = Guid.NewGuid().ToString("N"), Version = "3", Created =DateTime.UtcNow},
                };

            _containerRunner.Setup(p => p.GetApplicationVersions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(data);
            _containerRunner.SetupSequence(p => p.IsApplicationRunning(It.IsAny<ImageVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunnerState { IsRunning = true, Id = data[0].Id })
                .ReturnsAsync(new RunnerState { IsRunning = true, Id = data[1].Id })
                .ReturnsAsync(new RunnerState { IsRunning = false, Id = data[2].Id });
            _containerRunner.Setup(p => p.StopApplication(It.IsAny<RunnerState>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _configurationService.Setup(p => p.Configurations.Runner).Returns(Runner.Docker);

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            await service.Stop();

            _logger.VerifyLogging($"{Strings.ApplicationName} with container ID {data[0].Id} stopped.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{Strings.ApplicationName} with container ID {data[1].Id} stopped.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{Strings.ApplicationName} with container ID {data[2].Id} stopped.", LogLevel.Information, Times.Never());
        }

        [Fact(DisplayName = "Restart")]
        public async Task Restart()
        {
            var data = new List<ImageVersion>
                {
                    new ImageVersion{  Id = Guid.NewGuid().ToString("N"), Version = "1", Created =DateTime.UtcNow},
                    new ImageVersion{  Id = Guid.NewGuid().ToString("N"), Version = "2", Created =DateTime.UtcNow},
                };
            _containerRunner.Setup(p => p.GetApplicationVersions(It.IsAny<CancellationToken>()))
                .ReturnsAsync(data);
            _containerRunner.Setup(p => p.IsApplicationRunning(It.Is<ImageVersion>(p => p == data[0]), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunnerState { IsRunning = true, Id = Guid.NewGuid().ToString("N") });
            _containerRunner.Setup(p => p.GetLatestApplicationVersion(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(data[1]);
            _containerRunner.Setup(p => p.IsApplicationRunning(It.Is<ImageVersion>(p => p == data[1]), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunnerState { IsRunning = false, Id = Guid.NewGuid().ToString("N") });
            _containerRunner.Setup(p => p.StartApplication(It.IsAny<ImageVersion>(), It.IsAny<CancellationToken>()));
            _containerRunner.Setup(p => p.StopApplication(It.IsAny<RunnerState>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _configurationService.Setup(p => p.Configurations.Runner).Returns(Runner.Docker);

            var service = new ControlService(_containerRunnerFactory.Object, _logger.Object, _configurationService.Object);

            await service.Restart();
        }
    }
}
