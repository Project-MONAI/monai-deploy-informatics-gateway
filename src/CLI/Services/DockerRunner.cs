// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public class DockerRunner : IContainerRunner
    {
        private readonly ILogger<DockerRunner> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly IFileSystem _fileSystem;
        private readonly IDockerClient _dockerClient;

        public DockerRunner(ILogger<DockerRunner> logger, IConfigurationService configurationService, IFileSystem fileSystem, IDockerClient dockerClient)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
        }

        public async Task<RunnerState> IsApplicationRunning(ImageVersion imageVersion, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(imageVersion, nameof(imageVersion));

            _logger.CheckingExistingAppContainer(Strings.ApplicationName, imageVersion.Version);
            var parameters = new ContainersListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["ancestor"] = new Dictionary<string, bool>
                    {
                        [imageVersion.Id] = true
                    }
                }
            };
            var matches = await _dockerClient.Containers.ListContainersAsync(parameters, cancellationToken).ConfigureAwait(false);
            if (matches is null || matches.Count == 0)
            {
                return new RunnerState { IsRunning = false };
            }

            return new RunnerState { IsRunning = true, Id = matches.First().ID };
        }

        public async Task<ImageVersion> GetLatestApplicationVersion(CancellationToken cancellationToken = default)
            => await GetLatestApplicationVersion(_configurationService.Configurations.DockerImagePrefix, cancellationToken).ConfigureAwait(false);

        public async Task<ImageVersion> GetLatestApplicationVersion(string version, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));

            var results = await GetApplicationVersions(version, cancellationToken).ConfigureAwait(false);
            return results?.OrderByDescending(p => p.Created).FirstOrDefault();
        }

        public async Task<IList<ImageVersion>> GetApplicationVersions(CancellationToken cancellationToken = default)
            => await GetApplicationVersions(_configurationService.Configurations.DockerImagePrefix, cancellationToken).ConfigureAwait(false);

        public async Task<IList<ImageVersion>> GetApplicationVersions(string version, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));

            _logger.ConnectingToDocker();
            var parameters = new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool>
                    {
                        [version] = true
                    }
                }
            };
            _logger.RetrievingImagesFromDocker();
            var images = await _dockerClient.Images.ListImagesAsync(parameters, cancellationToken).ConfigureAwait(false);
            return images?.Select(p => new ImageVersion { Version = p.RepoTags.First(), Id = p.ID, Created = p.Created }).ToList();
        }

        public async Task<bool> StartApplication(ImageVersion imageVersion, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(imageVersion, nameof(imageVersion));

            _logger.CreatingDockerContainer(Strings.ApplicationName, imageVersion.Version, imageVersion.IdShort);
            var createContainerParams = new CreateContainerParameters() { Image = imageVersion.Id, HostConfig = new HostConfig() };

            createContainerParams.ExposedPorts = new Dictionary<string, EmptyStruct>();
            createContainerParams.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();

            _logger.DockerPrtBinding(_configurationService.Configurations.DicomListeningPort);
            createContainerParams.ExposedPorts.Add($"{_configurationService.Configurations.DicomListeningPort}/tcp", new EmptyStruct());
            createContainerParams.HostConfig.PortBindings.Add($"{_configurationService.Configurations.DicomListeningPort}/tcp", new List<PortBinding> { new PortBinding { HostPort = $"{_configurationService.Configurations.DicomListeningPort}" } });

            _logger.DockerPrtBinding(_configurationService.Configurations.InformaticsGatewayServerPort);
            createContainerParams.ExposedPorts.Add($"{_configurationService.Configurations.InformaticsGatewayServerPort}/tcp", new EmptyStruct());
            createContainerParams.HostConfig.PortBindings.Add($"{_configurationService.Configurations.InformaticsGatewayServerPort}/tcp", new List<PortBinding> { new PortBinding { HostPort = $"{_configurationService.Configurations.InformaticsGatewayServerPort}" } });

            createContainerParams.HostConfig.Mounts = new List<Mount>();
            _logger.DockerMountConfigFile(Common.ConfigFilePath, Common.MountedConfigFilePath);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = true, Source = Common.ConfigFilePath, Target = Common.MountedConfigFilePath });

            _logger.DockerMountDatabase(_configurationService.Configurations.HostDatabaseStorageMount, Common.MountedDatabasePath);
            _fileSystem.Directory.CreateDirectoryIfNotExists(_configurationService.Configurations.HostDatabaseStorageMount);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = false, Source = _configurationService.Configurations.HostDatabaseStorageMount, Target = Common.MountedDatabasePath });

            _logger.DockerMountTempDirectory(_configurationService.Configurations.HostDataStorageMount, _configurationService.Configurations.TempStoragePath);
            _fileSystem.Directory.CreateDirectoryIfNotExists(_configurationService.Configurations.HostDataStorageMount);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = false, Source = _configurationService.Configurations.HostDataStorageMount, Target = _configurationService.Configurations.TempStoragePath });

            _logger.DockerMountAppLogs(_configurationService.Configurations.HostLogsStorageMount, _configurationService.Configurations.LogStoragePath);
            _fileSystem.Directory.CreateDirectoryIfNotExists(_configurationService.Configurations.HostLogsStorageMount);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = false, Source = _configurationService.Configurations.HostLogsStorageMount, Target = _configurationService.Configurations.LogStoragePath });

            var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParams, cancellationToken).ConfigureAwait(false);
            var containerIdShort = response.ID.Substring(0, 12);

            _logger.DockerContainerCreated(Strings.ApplicationName, containerIdShort);
            if (response.Warnings.Any())
            {
                _logger.DockerCreateWarnings(string.Join(",", response.Warnings));
            }

            _logger.DockerStartContainer(containerIdShort);
            var containerStartParams = new ContainerStartParameters();
            if (!await _dockerClient.Containers.StartContainerAsync(response.ID, containerStartParams, cancellationToken).ConfigureAwait(false))
            {
                _logger.DockerContainerStartError(containerIdShort);
                return false;
            }
            else
            {
                _logger.DockerContainerStarted(Strings.ApplicationName, containerIdShort);
                return true;
            }
        }

        public async Task<bool> StopApplication(RunnerState runnerState, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(runnerState, nameof(runnerState));

            _logger.DockerContainerStopping(Strings.ApplicationName, runnerState.IdShort);
            var result = await _dockerClient.Containers.StopContainerAsync(runnerState.Id, new ContainerStopParameters() { WaitBeforeKillSeconds = 60 }, cancellationToken).ConfigureAwait(false);
            _logger.DockerContainerStopped(Strings.ApplicationName, runnerState.IdShort);
            return result;
        }
    }
}
