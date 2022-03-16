// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
                await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            _logger.Log(LogLevel.Information, $"{Common.ConfigFilePath} updated successfully.");
        }
    }
}
