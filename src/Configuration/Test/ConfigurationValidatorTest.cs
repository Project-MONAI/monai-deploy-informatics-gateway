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
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Configuration.Test
{
    public class ConfigurationValidatorTest
    {
        private readonly Mock<ILogger<ConfigurationValidator>> _logger;
        private readonly Mock<IFileSystem> _fileSystem;

        public ConfigurationValidatorTest()
        {
            _logger = new Mock<ILogger<ConfigurationValidator>>();
            _fileSystem = new Mock<IFileSystem>();
            _fileSystem.Setup(p => p.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystem.Setup(p => p.File.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<FileOptions>())).Returns(FileStream.Null);
        }

        [Fact(DisplayName = "ConfigurationValidator test with all valid settings")]
        public void AllValid()
        {
            var config = MockValidConfiguration();
            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);
            Assert.True(valid == ValidateOptionsResult.Success);
        }

        [Fact(DisplayName = "ConfigurationValidator test with invalid SCP port number")]
        public void InvalidScpPort()
        {
            var config = MockValidConfiguration();
            config.Dicom.Scp.Port = Int32.MaxValue;

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessage = $"Invalid port number '{Int32.MaxValue}' specified for InformaticsGateway>dicom>scp>port.";
            Assert.Equal(validationMessage, valid.FailureMessage);
            _logger.VerifyLogging(validationMessage, LogLevel.Error, Times.Once());
        }

        [Fact(DisplayName = "ConfigurationValidator test with invalid maximum number of associations")]
        public void InvalidScpMaxAssociations()
        {
            var config = MockValidConfiguration();
            config.Dicom.Scp.MaximumNumberOfAssociations = 0;

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessage = $"Value of InformaticsGateway>dicom>scp>max-associations must be between {1} and {1000}.";
            Assert.Equal(validationMessage, valid.FailureMessage);
            _logger.VerifyLogging(validationMessage, LogLevel.Error, Times.Once());
        }

        [Fact(DisplayName = "ConfigurationValidator test with invalid storage watermark")]
        public void StorageWithInvalidWatermark()
        {
            var config = MockValidConfiguration();
            config.Storage.Watermark = 1000;

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessage = "Value of InformaticsGateway>storage>watermark must be between 1 and 100.";
            Assert.Equal(validationMessage, valid.FailureMessage);
            _logger.VerifyLogging(validationMessage, LogLevel.Error, Times.Once());
        }

        [Fact(DisplayName = "ConfigurationValidator test with invalid reserved space")]
        public void StorageWithInvalidReservedSpace()
        {
            var config = MockValidConfiguration();
            config.Storage.ReserveSpaceGB = 9999;

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessage = "Value of InformaticsGateway>storage>reserveSpaceGB must be between 1 and 999.";
            Assert.Equal(validationMessage, valid.FailureMessage);
            _logger.VerifyLogging(validationMessage, LogLevel.Error, Times.Once());
        }

        [Fact(DisplayName = "ConfigurationValidator test with missing temporaryBucketName")]
        public void StorageWithInvalidTemporaryBucketName()
        {
            var config = MockValidConfiguration();
            config.Storage.TemporaryStorageBucket = " ";

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessages = new[] { "Value for InformaticsGateway>storage>temporaryBucketName is required.", "Value for InformaticsGateway>storage>temporaryBucketName does not conform to Amazon S3 bucket naming requirements." };
            Assert.Equal(string.Join(Environment.NewLine, validationMessages), valid.FailureMessage);
            foreach (var message in validationMessages)
            {
                _logger.VerifyLogging(message, LogLevel.Error, Times.Once());
            }
        }

        [Fact(DisplayName = "ConfigurationValidator test with missing bucketName")]
        public void StorageWithInvalidBucketName()
        {
            var config = MockValidConfiguration();
            config.Storage.StorageServiceBucketName = "";

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessages = new[] { "Value for InformaticsGateway>storage>bucketName is required.", "Value for InformaticsGateway>storage>bucketName does not conform to Amazon S3 bucket naming requirements." };
            Assert.Equal(string.Join(Environment.NewLine, validationMessages), valid.FailureMessage);
            foreach (var message in validationMessages)
            {
                _logger.VerifyLogging(message, LogLevel.Error, Times.Once());
            }
        }

        [Fact(DisplayName = "ConfigurationValidator test with inaccessible directory")]
        public void StorageWithInaccessbleDirectory()
        {
            _fileSystem.Setup(p => p.File.Create(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<FileOptions>())).Throws(new UnauthorizedAccessException("error"));

            var config = MockValidConfiguration();
            config.Storage.TemporaryDataStorage = TemporaryDataStorageLocation.Disk;
            config.Storage.BufferStorageRootPath = "/blabla";

            var valid = new ConfigurationValidator(_logger.Object, _fileSystem.Object).Validate("", config);

            var validationMessages = new[] { $"Directory `/blabla` specified in `InformaticsGateway>storage>bufferRootPath` is not accessible: error." };
            Assert.Equal(string.Join(Environment.NewLine, validationMessages), valid.FailureMessage);
            foreach (var message in validationMessages)
            {
                _logger.VerifyLogging(message, LogLevel.Error, Times.Once());
            }
        }

        private static InformaticsGatewayConfiguration MockValidConfiguration()
        {
            var config = new InformaticsGatewayConfiguration();
            config.Dicom.Scp.RejectUnknownSources = true;
            config.Storage.Watermark = 50;
            config.Storage.TemporaryStorageBucket = "temp-bucket";
            config.Storage.StorageServiceBucketName = "bucket";
            return config;
        }
    }
}
