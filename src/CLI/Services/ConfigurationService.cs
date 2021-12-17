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
using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

        public ConfigurationService(ILogger<ConfigurationService> logger, IFileSystem fileSystem, IEmbeddedResource embeddedResource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _embeddedResource = embeddedResource ?? throw new ArgumentNullException(nameof(embeddedResource));
            Configurations = new ConfigurationOptionAccessor(fileSystem);
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
            _logger.Log(LogLevel.Debug, $"Reading default application configurations...");
            using var stream = _embeddedResource.GetManifestResourceStream(Common.AppSettingsResourceName);

            if (stream is null)
            {
                _logger.Log(LogLevel.Debug, $"Available manifest names {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
                throw new ConfigurationException($"Default configuration file could not be loaded, please reinstall the CLI.");
            }
            CreateConfigDirectoryIfNotExist();

            _logger.Log(LogLevel.Information, $"Saving appsettings.json to {Common.ConfigFilePath}...");
            using (var fileStream = _fileSystem.FileStream.Create(Common.ConfigFilePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            }
            this._logger.Log(LogLevel.Information, $"{Common.ConfigFilePath} updated successfully.");
        }
    }
}
