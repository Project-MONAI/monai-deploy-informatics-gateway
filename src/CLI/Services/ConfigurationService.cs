/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IEmbeddedResource _embeddedResource;

        public bool IsInitialized => _fileSystem.Directory.Exists(Common.MigDirectory) && IsConfigExists;

        public bool IsConfigExists => _fileSystem.File.Exists(Common.ConfigFilePath);

        public IConfigurationOptionAccessor Configurations { get; }
        public INLogConfigurationOptionAccessor NLogConfigurations { get; }

        public ConfigurationService(
            ILogger<ConfigurationService> logger,
            IFileSystem fileSystem,
            IEmbeddedResource embeddedResource,
            IConfigurationOptionAccessor configurationOptionAccessor,
            INLogConfigurationOptionAccessor nLogConfigurationOptionAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _embeddedResource = embeddedResource ?? throw new ArgumentNullException(nameof(embeddedResource));
            Configurations = configurationOptionAccessor ?? throw new ArgumentNullException(nameof(configurationOptionAccessor));
            NLogConfigurations = nLogConfigurationOptionAccessor ?? throw new ArgumentNullException(nameof(nLogConfigurationOptionAccessor));
        }

        public void CreateConfigDirectoryIfNotExist()
        {
            if (!_fileSystem.Directory.Exists(Common.MigDirectory))
            {
                _fileSystem.Directory.CreateDirectory(Common.MigDirectory);
            }
        }

        public async Task Initialize(CancellationToken cancellationToken)
        {
            _logger.DebugMessage("Reading default application configurations...");
            await WriteConfigFile(Common.AppSettingsResourceName, Common.ConfigFilePath, cancellationToken).ConfigureAwait(false);
            await WriteConfigFile(Common.NLogConfigResourceName, Common.NLogConfigFilePath, cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteConfigFile(string resourceName, string outputPath, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(resourceName, nameof(resourceName));
            Guard.Against.NullOrWhiteSpace(outputPath, nameof(outputPath));

            using var stream = _embeddedResource.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                _logger.AvailableManifest(string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames()));
                throw new ConfigurationException($"Default configuration file: {resourceName} could not be loaded, please reinstall the CLI.");
            }
            CreateConfigDirectoryIfNotExist();

            _logger.SaveAppSettings(resourceName, outputPath);
            using (var fileStream = _fileSystem.FileStream.Create(outputPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            _logger.AppSettingUpdated(outputPath);

        }
    }
}
