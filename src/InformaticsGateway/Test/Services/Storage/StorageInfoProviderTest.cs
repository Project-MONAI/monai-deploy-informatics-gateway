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

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Storage
{
    public class StorageInfoProviderTest
    {
        private const long OneGB = 1000000000;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<ILogger<StorageInfoProvider>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<IDriveInfo> _driveInfo;

        public StorageInfoProviderTest()
        {
            _fileSystem = new Mock<IFileSystem>();
            _logger = new Mock<ILogger<StorageInfoProvider>>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _driveInfo = new Mock<IDriveInfo>();

            _fileSystem.Setup(p => p.DriveInfo.FromDriveName(It.IsAny<string>()))
                    .Returns(_driveInfo.Object);
            _fileSystem.Setup(p => p.Directory.CreateDirectory(It.IsAny<string>()));
        }

        [RetryFact(5, 250, DisplayName = "Available free space")]
        public void AvailableFreeSpace()
        {
            var totalSize = 10 * OneGB;
            var freeSpace = 9 * OneGB;
            _driveInfo.Setup(p => p.AvailableFreeSpace).Returns(freeSpace);
            _driveInfo.Setup(p => p.TotalSize).Returns(totalSize);
            _configuration.Value.Storage.Watermark = 10;
            _configuration.Value.Storage.ReserveSpaceGB = 1;

            var storageInfoProvider = new StorageInfoProvider(_configuration, _fileSystem.Object, _logger.Object);

            Assert.Equal(freeSpace, storageInfoProvider.AvailableFreeSpace);
            _logger.VerifyLogging($"Storage Size: {totalSize:N0}. Reserved: {(9 * OneGB):N0}.", LogLevel.Information, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Space is available...")]
        public void HasSpaceAvailableTo()
        {
            var totalSize = 10 * OneGB;
            var freeSpace = 9 * OneGB;
            _driveInfo.Setup(p => p.AvailableFreeSpace).Returns(freeSpace);
            _driveInfo.Setup(p => p.TotalSize).Returns(totalSize);
            _configuration.Value.Storage.Watermark = 90;
            _configuration.Value.Storage.ReserveSpaceGB = 1;
            var storageInfoProvider = new StorageInfoProvider(_configuration, _fileSystem.Object, _logger.Object);

            Assert.True(storageInfoProvider.HasSpaceAvailableForExport);
            Assert.True(storageInfoProvider.HasSpaceAvailableToRetrieve);
            Assert.True(storageInfoProvider.HasSpaceAvailableToStore);

            _logger.VerifyLogging($"Storage Size: {totalSize:N0}. Reserved: {(OneGB):N0}. Available: {freeSpace:N0}.", LogLevel.Debug, Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "Space usage is above watermark")]
        public void SpaceUsageAboveWatermark()
        {
            var totalSize = 10 * OneGB;
            var freeSpace = 5 * OneGB;
            _driveInfo.Setup(p => p.AvailableFreeSpace).Returns(freeSpace);
            _driveInfo.Setup(p => p.TotalSize).Returns(totalSize);
            _configuration.Value.Storage.Watermark = 10;
            _configuration.Value.Storage.ReserveSpaceGB = 1;
            var storageInfoProvider = new StorageInfoProvider(_configuration, _fileSystem.Object, _logger.Object);

            Assert.False(storageInfoProvider.HasSpaceAvailableForExport);
            Assert.False(storageInfoProvider.HasSpaceAvailableToRetrieve);
            Assert.False(storageInfoProvider.HasSpaceAvailableToStore);

            _logger.VerifyLogging($"Storage Size: {totalSize:N0}. Reserved: {(9 * OneGB):N0}. Available: {freeSpace:N0}.", LogLevel.Information, Times.Exactly(3));
        }

        [RetryFact(5, 250, DisplayName = "Reserved space is low")]
        public void ReservedSpaceIsLow()
        {
            var totalSize = 10 * OneGB;
            var freeSpace = 5 * OneGB;
            _driveInfo.Setup(p => p.AvailableFreeSpace).Returns(freeSpace);
            _driveInfo.Setup(p => p.TotalSize).Returns(totalSize);
            _configuration.Value.Storage.Watermark = 99;
            _configuration.Value.Storage.ReserveSpaceGB = 9;
            var storageInfoProvider = new StorageInfoProvider(_configuration, _fileSystem.Object, _logger.Object);

            Assert.False(storageInfoProvider.HasSpaceAvailableForExport);
            Assert.False(storageInfoProvider.HasSpaceAvailableToRetrieve);
            Assert.False(storageInfoProvider.HasSpaceAvailableToStore);

            _logger.VerifyLogging($"Storage Size: {totalSize:N0}. Reserved: {(9 * OneGB):N0}. Available: {freeSpace:N0}.", LogLevel.Information, Times.Exactly(3));
        }
    }
}
