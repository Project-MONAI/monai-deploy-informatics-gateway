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

using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Moq;
using System;
using System.IO.Abstractions;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ContainerRunnerFactoryTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<ILogger<DockerRunner>> _logger;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IDockerClient> _dockerClient;

        public ContainerRunnerFactoryTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _configurationService = new Mock<IConfigurationService>();
            _serviceScope = new Mock<IServiceScope>();
            _logger = new Mock<ILogger<DockerRunner>>();
            _fileSystem = new Mock<IFileSystem>();
            _dockerClient = new Mock<IDockerClient>();
        }

        [Fact(DisplayName = "ContainerRunnerFactory Constructor")]
        public void ContainerRunnerFactory_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new ContainerRunnerFactory(null, null));
            Assert.Throws<ArgumentNullException>(() => new ContainerRunnerFactory(_serviceScopeFactory.Object, null));
        }

        [Fact(DisplayName = "GetContainerRunner")]
        public void GetContainerRunner()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            _configurationService.SetupGet(p => p.Configurations.Runner).Returns(Runner.Docker);
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider.GetService(It.IsAny<Type>())).Returns(runner);
            var factory = new ContainerRunnerFactory(_serviceScopeFactory.Object, _configurationService.Object);

            var result = factory.GetContainerRunner();
            Assert.Equal(result, runner);
        }

        [Fact(DisplayName = "GetContainerRunner NotImplementedException")]
        public void GetContainerRunner_NotImplementedException()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            _configurationService.SetupGet(p => p.Configurations.Runner).Returns(Runner.Helm);
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            var factory = new ContainerRunnerFactory(_serviceScopeFactory.Object, _configurationService.Object);

            Assert.Throws<NotImplementedException>(() => factory.GetContainerRunner());
        }
    }
}
