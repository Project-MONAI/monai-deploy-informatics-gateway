/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

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
            _logger.TemporaryStoragePath(_storageConfiguration.TemporaryDataDirFullPath);

            Initialize();
        }

        private void Initialize()
        {
            var driveInfo = _fileSystem.DriveInfo.FromDriveName(_storageConfiguration.TemporaryDataDirFullPath);
            _reservedSpace = (long)(driveInfo.TotalSize * (1 - (_storageConfiguration.Watermark / 100.0)));
            _reservedSpace = Math.Max(_reservedSpace, _storageConfiguration.ReserveSpaceGB * OneGB);
            _logger.StorageSizeWithReserve(driveInfo.TotalSize, _reservedSpace);
        }

        private bool IsSpaceAvailable()
        {
            var driveInfo = _fileSystem.DriveInfo.FromDriveName(_storageConfiguration.TemporaryDataDirFullPath);

            var freeSpace = driveInfo.AvailableFreeSpace;

            if (freeSpace <= _reservedSpace)
            {
                _logger.StorageSizeWithReserveAndAvailable(driveInfo.TotalSize, _reservedSpace, freeSpace);
            }

            return freeSpace > _reservedSpace;
        }
    }
}
