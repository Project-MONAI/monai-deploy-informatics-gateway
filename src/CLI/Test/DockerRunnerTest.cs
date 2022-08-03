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
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class DockerRunnerTest
    {
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly Mock<IDockerClient> _dockerClient;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<ILogger<DockerRunner>> _logger;

        public DockerRunnerTest()
        {
            _logger = new Mock<ILogger<DockerRunner>>();
            _configurationService = new Mock<IConfigurationService>();
            _dockerClient = new Mock<IDockerClient>();
            _fileSystem = new Mock<IFileSystem>();
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "DockerRunner Constructor")]
        public void DockerRunner_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new DockerRunner(null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DockerRunner(_logger.Object, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new DockerRunner(_logger.Object, _configurationService.Object, null, null));
            Assert.Throws<ArgumentNullException>(() => new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, null));
        }

        [Fact(DisplayName = "GetApplicationVersions")]
        public async Task GetApplicationVersions()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            var data = new List<ImagesListResponse>
                {
                    new ImagesListResponse{ RepoTags = new List<string>{ "123"}, ID = $"sha256:{Guid.NewGuid():N}", Created = DateTime.Now },
                    new ImagesListResponse{ RepoTags = new List<string>{ "456"}, ID = $"sha256:{Guid.NewGuid():N}", Created = DateTime.Now }
                };

            _configurationService.SetupGet(p => p.Configurations.DockerImagePrefix).Returns("PREFIX");
            _dockerClient.SetupSequence(p => p.Images.ListImagesAsync(It.IsAny<ImagesListParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(data)
                .ReturnsAsync(default(List<ImagesListResponse>));

            var results = await runner.GetApplicationVersions(CancellationToken.None);

            Assert.Equal(2, results.Count);

            results = await runner.GetApplicationVersions(CancellationToken.None);
            Assert.Null(results);

            _dockerClient.Verify(
                p => p.Images.ListImagesAsync(It.Is<ImagesListParameters>(
                    p => p.Filters.ContainsKey("reference") && p.Filters["reference"].ContainsKey("PREFIX")), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact(DisplayName = "GetLatestApplicationVersion")]
        public async Task GetLatestApplicationVersion()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            var id = Guid.NewGuid().ToString("N");
            var shaId = $"sha256:{id}";

            _configurationService.SetupGet(p => p.Configurations.DockerImagePrefix).Returns("PREFIX");
            _dockerClient.SetupSequence(p => p.Images.ListImagesAsync(It.IsAny<ImagesListParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ImagesListResponse>
                {
                    new ImagesListResponse{ RepoTags = new List<string>{ "123"}, ID = shaId, Created = DateTime.Now }
                })
                .ReturnsAsync(default(List<ImagesListResponse>));

            var result = await runner.GetLatestApplicationVersion(CancellationToken.None);

            Assert.Equal("123", result.Version);
            Assert.Equal(shaId, result.Id);
            Assert.Equal(id.Substring(0, 12), result.IdShort);

            result = await runner.GetLatestApplicationVersion(CancellationToken.None);
            Assert.Null(result);

            _dockerClient.Verify(
                p => p.Images.ListImagesAsync(It.Is<ImagesListParameters>(
                    p => p.Filters.ContainsKey("reference") && p.Filters["reference"].ContainsKey("PREFIX")), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact(DisplayName = "IsApplicationRunning")]
        public async Task IsApplicationRunning()
        {
            var image = new ImageVersion { Id = Guid.NewGuid().ToString("N") };
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            var id = Guid.NewGuid().ToString("N");
            _dockerClient.SetupSequence(p => p.Containers.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContainerListResponse>
                {
                    new ContainerListResponse{ ID = id}
                })
                .ReturnsAsync(default(List<ContainerListResponse>));

            var result = await runner.IsApplicationRunning(image, CancellationToken.None);

            Assert.True(result.IsRunning);
            Assert.Equal(id, result.Id);
            Assert.Equal(id.Substring(0, 12), result.IdShort);

            result = await runner.IsApplicationRunning(image, CancellationToken.None);
            Assert.False(result.IsRunning);

            _dockerClient.Verify(
                p => p.Containers.ListContainersAsync(It.Is<ContainersListParameters>(
                    p => p.Filters.ContainsKey("ancestor") && p.Filters["ancestor"].ContainsKey(image.Id)), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact(DisplayName = "StartApplication")]
        public async Task StartApplication()
        {
            var image = new ImageVersion { Id = Guid.NewGuid().ToString("N") };
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            var createContainerResponse = new CreateContainerResponse { ID = Guid.NewGuid().ToString("N"), Warnings = new List<string>() };
            var createContainerResponseWithWarning = new CreateContainerResponse { ID = Guid.NewGuid().ToString("N"), Warnings = new List<string>() { "warning1" } };

            _configurationService.SetupGet(p => p.Configurations.DockerImagePrefix).Returns("PREFIX");
            _dockerClient.SetupSequence(p => p.Containers.CreateContainerAsync(It.IsAny<CreateContainerParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createContainerResponse)
                .ReturnsAsync(createContainerResponseWithWarning);

            _dockerClient.SetupSequence(p => p.Containers.StartContainerAsync(It.IsAny<string>(), It.IsAny<ContainerStartParameters>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true)
               .ReturnsAsync(false);

            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(true);
            _configurationService.SetupGet(p => p.Configurations.DicomListeningPort).Returns(100);
            _configurationService.SetupGet(p => p.Configurations.InformaticsGatewayServerPort).Returns(200);
            _configurationService.SetupGet(p => p.Configurations.HostDatabaseStorageMount).Returns("/database");
            _configurationService.SetupGet(p => p.Configurations.HostDataStorageMount).Returns("/storage");
            _configurationService.SetupGet(p => p.Configurations.HostLogsStorageMount).Returns("/logs");
            _configurationService.SetupGet(p => p.Configurations.HostPlugInsStorageMount).Returns("/plug-ins");
            _configurationService.SetupGet(p => p.Configurations.TempStoragePath).Returns("/tempdata");
            _configurationService.SetupGet(p => p.Configurations.LogStoragePath).Returns("/templogs");

            Assert.True(await runner.StartApplication(image, CancellationToken.None));
            Assert.False(await runner.StartApplication(image, CancellationToken.None));

            _dockerClient.Verify(
                p => p.Containers.CreateContainerAsync(It.Is<CreateContainerParameters>(
                    c => c.HostConfig.PortBindings.ContainsKey("100/tcp") &&
                        c.ExposedPorts.ContainsKey("100/tcp") &&
                        c.HostConfig.PortBindings.ContainsKey("200/tcp") &&
                        c.ExposedPorts.ContainsKey("200/tcp") &&
                        c.HostConfig.Mounts.Count(m => m.ReadOnly && m.Source == Common.ConfigFilePath && m.Target == Common.MountedConfigFilePath) == 1 &&
                        c.HostConfig.Mounts.Count(m => !m.ReadOnly && m.Source == "/database" && m.Target == Common.MountedDatabasePath) == 1 &&
                        c.HostConfig.Mounts.Count(m => !m.ReadOnly && m.Source == "/storage" && m.Target == "/tempdata") == 1 &&
                        c.HostConfig.Mounts.Count(m => !m.ReadOnly && m.Source == "/logs" && m.Target == "/templogs") == 1), It.IsAny<CancellationToken>()), Times.Exactly(2));

            _logger.VerifyLogging("Warnings: warning1", LogLevel.Warning, Times.Once());
        }

        [Fact(DisplayName = "StopApplication")]
        public async Task StopApplication()
        {
            var runner = new DockerRunner(_logger.Object, _configurationService.Object, _fileSystem.Object, _dockerClient.Object);
            var runnerState = new RunnerState { Id = Guid.NewGuid().ToString("N"), IsRunning = true };

            _dockerClient.SetupSequence(p => p.Containers.StopContainerAsync(It.IsAny<string>(), It.IsAny<ContainerStopParameters>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            Assert.True(await runner.StopApplication(runnerState, CancellationToken.None));
            Assert.False(await runner.StopApplication(runnerState, CancellationToken.None));

            _dockerClient.Verify(
                p => p.Containers.StopContainerAsync(runnerState.Id, It.IsAny<ContainerStopParameters>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
