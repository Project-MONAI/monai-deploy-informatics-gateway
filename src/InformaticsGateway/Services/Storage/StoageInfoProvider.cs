// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    public class StorageInfoProvider : IStorageInfoProvider
    {
        private const long OneGB = 1000000000;
        private readonly StorageConfiguration _storageConfiguration;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<StorageInfoProvider> _logger;
        private long _reservedSpace;

        public bool HasSpaceAvailableToStore { get => IsSpaceAvailable(); }

        public bool HasSpaceAvailableForExport { get => IsSpaceAvailable(); }

        public bool HasSpaceAvailableToRetrieve { get => IsSpaceAvailable(); }

        public long AvailableFreeSpace
        {
            get
            {
                var driveInfo = _fileSystem.DriveInfo.FromDriveName(_storageConfiguration.TemporaryDataDirFullPath);
                return driveInfo.AvailableFreeSpace;
            }
        }

        public StorageInfoProvider(
            IOptions<InformaticsGatewayConfiguration> configuration,
            IFileSystem fileSystem,
            ILogger<StorageInfoProvider> logger)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _storageConfiguration = configuration.Value.Storage;
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (!_fileSystem.Directory.Exists(_storageConfiguration.TemporaryDataDirFullPath))
            {
                _fileSystem.Directory.CreateDirectory(_storageConfiguration.TemporaryDataDirFullPath);
            }
            _logger.Log(LogLevel.Information, $"Temporary Storage Path={_storageConfiguration.TemporaryDataDirFullPath}.");

            Initialize();
        }

        private void Initialize()
        {
            var driveInfo = _fileSystem.DriveInfo.FromDriveName(_storageConfiguration.TemporaryDataDirFullPath);
            _reservedSpace = (long)(driveInfo.TotalSize * (1 - (_storageConfiguration.Watermark / 100.0)));
            _reservedSpace = Math.Max(_reservedSpace, _storageConfiguration.ReserveSpaceGB * OneGB);
            _logger.Log(LogLevel.Information, $"Storage Size: {driveInfo.TotalSize:N0}. Reserved: {_reservedSpace:N0}.");
        }

        private bool IsSpaceAvailable()
        {
            var driveInfo = _fileSystem.DriveInfo.FromDriveName(_storageConfiguration.TemporaryDataDirFullPath);

            var freeSpace = driveInfo.AvailableFreeSpace;

            if (freeSpace <= _reservedSpace)
            {
                _logger.Log(LogLevel.Information, $"Storage Size: {driveInfo.TotalSize:N0}. Reserved: {_reservedSpace:N0}. Available: {freeSpace:N0}.");
            }

            return freeSpace > _reservedSpace;
        }
    }
}
