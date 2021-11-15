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

using Ardalis.GuardClauses;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class DockerRunner : IContainerRunner
    {
        private readonly ILogger<DockerRunner> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly IFileSystem _fileSystem;
        public readonly IDockerClient _dockerClient;

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

            _logger.Log(LogLevel.Debug, $"Checking for existing {Strings.ApplicationName} ({imageVersion.Version}) containers...");
            var parameters = new ContainersListParameters();
            parameters.Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["ancestor"] = new Dictionary<string, bool>
                {
                    [imageVersion.Id] = true
                }
            };
            var matches = await _dockerClient.Containers.ListContainersAsync(parameters, cancellationToken);
            if (matches is null || matches.Count() == 0)
            {
                return new RunnerState { IsRunning = false };
            }

            return new RunnerState { IsRunning = true, Id = matches.First().ID };
        }

        public async Task<ImageVersion> GetLatestApplicationVersion(CancellationToken cancellationToken = default)
            => await GetLatestApplicationVersion(_configurationService.Configurations.DockerImagePrefix, cancellationToken);

        public async Task<ImageVersion> GetLatestApplicationVersion(string version, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));

            var results = await GetApplicationVersions(version, cancellationToken);
            return results?.OrderByDescending(p => p.Created).FirstOrDefault();
        }

        public async Task<IList<ImageVersion>> GetApplicationVersions(CancellationToken cancellationToken = default)
            => await GetApplicationVersions(_configurationService.Configurations.DockerImagePrefix, cancellationToken);

        public async Task<IList<ImageVersion>> GetApplicationVersions(string label, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(label, nameof(label));

            _logger.Log(LogLevel.Debug, "Connecting to Docker...");
            var parameters = new ImagesListParameters();
            parameters.Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["reference"] = new Dictionary<string, bool>
                {
                    [label] = true
                }
            };
            _logger.Log(LogLevel.Debug, "Retrieving images from Docker...");
            var images = await _dockerClient.Images.ListImagesAsync(parameters, cancellationToken);
            return images?.Select(p => new ImageVersion { Version = p.RepoTags.First(), Id = p.ID, Created = p.Created }).ToList();
        }

        public async Task<bool> StartApplication(ImageVersion imageVersion, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(imageVersion, nameof(imageVersion));

            _logger.Log(LogLevel.Information, $"Creating container {Strings.ApplicationName} - {imageVersion.Version} ({imageVersion.IdShort})...");
            var createContainerParams = new CreateContainerParameters() { Image = imageVersion.Id, HostConfig = new HostConfig() };

            createContainerParams.ExposedPorts = new Dictionary<string, EmptyStruct>();
            createContainerParams.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();

            _logger.Log(LogLevel.Information, $"\tPort binding: {_configurationService.Configurations.DicomListeningPort}/tcp");
            createContainerParams.ExposedPorts.Add($"{_configurationService.Configurations.DicomListeningPort}/tcp", new EmptyStruct());
            createContainerParams.HostConfig.PortBindings.Add($"{_configurationService.Configurations.DicomListeningPort}/tcp", new List<PortBinding> { new PortBinding { HostPort = $"{_configurationService.Configurations.DicomListeningPort}" } });

            _logger.Log(LogLevel.Information, $"\tPort binding: {_configurationService.Configurations.InformaticsGatewayServerPort}/tcp");
            createContainerParams.ExposedPorts.Add($"{_configurationService.Configurations.InformaticsGatewayServerPort}/tcp", new EmptyStruct());
            createContainerParams.HostConfig.PortBindings.Add($"{_configurationService.Configurations.InformaticsGatewayServerPort}/tcp", new List<PortBinding> { new PortBinding { HostPort = $"{_configurationService.Configurations.InformaticsGatewayServerPort}" } });

            createContainerParams.HostConfig.Mounts = new List<Mount>();
            _logger.Log(LogLevel.Information, $"\tMount (configuration file): {Common.ConfigFilePath} => {Common.MountedConfigFilePath}");
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = true, Source = Common.ConfigFilePath, Target = Common.MountedConfigFilePath });

            _logger.Log(LogLevel.Information, $"\tMount (database file):      {_configurationService.Configurations.HostDatabaseStorageMount} => {Common.MountedDatabasePath}");
            _fileSystem.Directory.CreateDirectoryIfNotExists(_configurationService.Configurations.HostDatabaseStorageMount);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = false, Source = _configurationService.Configurations.HostDatabaseStorageMount, Target = Common.MountedDatabasePath });

            _logger.Log(LogLevel.Information, $"\tMount (temporary storage):  {_configurationService.Configurations.HostDataStorageMount} => {_configurationService.Configurations.TempStoragePath}");
            _fileSystem.Directory.CreateDirectoryIfNotExists(_configurationService.Configurations.HostDataStorageMount);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = false, Source = _configurationService.Configurations.HostDataStorageMount, Target = _configurationService.Configurations.TempStoragePath });

            _logger.Log(LogLevel.Information, $"\tMount (application logs):   {_configurationService.Configurations.HostLogsStorageMount} => {_configurationService.Configurations.LogStoragePath}");
            _fileSystem.Directory.CreateDirectoryIfNotExists(_configurationService.Configurations.HostLogsStorageMount);
            createContainerParams.HostConfig.Mounts.Add(new Mount { Type = "bind", ReadOnly = false, Source = _configurationService.Configurations.HostLogsStorageMount, Target = _configurationService.Configurations.LogStoragePath });

            var response = await _dockerClient.Containers.CreateContainerAsync(createContainerParams, cancellationToken);
            _logger.Log(LogLevel.Debug, $"{Strings.ApplicationName} created with container ID {response.ID.Substring(0, 12)}");
            if (response.Warnings.Any())
            {
                _logger.Log(LogLevel.Warning, $"Warnings: {string.Join(",", response.Warnings)}");
            }

            _logger.Log(LogLevel.Debug, $"Starting container {response.ID.Substring(0, 12)}...");
            var containerStartParams = new ContainerStartParameters();
            if (!await _dockerClient.Containers.StartContainerAsync(response.ID, containerStartParams, cancellationToken))
            {
                _logger.Log(LogLevel.Error, $"Error starting container {response.ID.Substring(0, 12)}");
                return false;
            }
            else
            {
                _logger.Log(LogLevel.Information, $"{Strings.ApplicationName} started with container ID {response.ID.Substring(0, 12)}");
                return true;
            }
        }

        public async Task<bool> StopApplication(RunnerState runnerState, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(runnerState, nameof(runnerState));

            _logger.Log(LogLevel.Debug, $"Stopping {Strings.ApplicationName} with container ID {runnerState.IdShort}.");
            var result = await _dockerClient.Containers.StopContainerAsync(runnerState.Id, new ContainerStopParameters() { WaitBeforeKillSeconds = 60 }, cancellationToken);
            _logger.Log(LogLevel.Information, $"{Strings.ApplicationName} with container ID {runnerState.IdShort} stopped.");
            return result;
        }
    }
}
