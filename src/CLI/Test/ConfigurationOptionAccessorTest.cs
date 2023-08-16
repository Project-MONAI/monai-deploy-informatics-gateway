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
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class ConfigurationOptionAccessorTest
    {
        public ConfigurationOptionAccessorTest()
        {
        }

        [Fact(DisplayName = "ConfigurationOptionAccessor Constructor")]
        public void DockerRunner_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigurationOptionAccessor(null));
        }

        [Fact]
        public void DicomListeningPort_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"InformaticsGateway\": {\"dicom\": {\"scp\": {\"port\": 104}}}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal(104, configurationOptionAccessor.DicomListeningPort);
        }

        [Fact]
        public void DicomListeningPort_Set_UpdatesValue()
        {
            var fileSystem = SetupFileSystem("{\"InformaticsGateway\": {\"dicom\": {\"scp\": {\"port\": 104}}}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);
            configurationOptionAccessor.DicomListeningPort = 1000;
            Assert.Equal(1000, configurationOptionAccessor.DicomListeningPort);
        }

        [Fact]
        public void Hl7ListeningPort_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"InformaticsGateway\": {\"hl7\": {\"port\": 2575}}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal(2575, configurationOptionAccessor.Hl7ListeningPort);
        }

        [Fact]
        public void Hl7ListeningPort_Set_UpdatesValue()
        {
            var fileSystem = SetupFileSystem("{\"InformaticsGateway\": {\"hl7\": {\"port\": 2575}}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);
            configurationOptionAccessor.Hl7ListeningPort = 1000;
            Assert.Equal(1000, configurationOptionAccessor.Hl7ListeningPort);
        }

        [Fact]
        public void DockerImagePrefix_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"DockerImagePrefix\": \"ghcr.io/project-monai/monai-deploy-informatics-gateway\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal("ghcr.io/project-monai/monai-deploy-informatics-gateway", configurationOptionAccessor.DockerImagePrefix);
        }

        [Fact]
        public void HostDatabaseStorageMount_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"HostDatabaseStorageMount\": \"~/.mig/database\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal($"{Common.HomeDir}/.mig/database", configurationOptionAccessor.HostDatabaseStorageMount);
        }

        [Fact]
        public void HostDataStorageMount_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"HostDataStorageMount\": \"~/.mig/data\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal($"{Common.HomeDir}/.mig/data", configurationOptionAccessor.HostDataStorageMount);
        }

        [Fact]
        public void HostPlugInsStorageMount_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"HostPlugInsStorageMount\": \"~/.mig/plug-ins\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal($"{Common.HomeDir}/.mig/plug-ins", configurationOptionAccessor.HostPlugInsStorageMount);
        }

        [Fact]
        public void HostLogsStorageMount_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"HostLogsStorageMount\": \"~/.mig/logs\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal($"{Common.HomeDir}/.mig/logs", configurationOptionAccessor.HostLogsStorageMount);
        }

        [Fact]
        public void InformaticsGatewayServerEndpoint_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"InformaticsGatewayServerEndpoint\": \"http://localhost:5000\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal("http://localhost:5000", configurationOptionAccessor.InformaticsGatewayServerEndpoint);
        }

        [Fact]
        public void InformaticsGatewayServerEndpoint_Set_UpdatesValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"InformaticsGatewayServerEndpoint\": \"http://localhost:5000\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);
            configurationOptionAccessor.InformaticsGatewayServerEndpoint = "http://hello-world";
            Assert.Equal("http://hello-world", configurationOptionAccessor.InformaticsGatewayServerEndpoint);
        }

        [Fact]
        public void InformaticsGatewayServerPort_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"InformaticsGatewayServerEndpoint\": \"http://localhost:5000\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal(5000, configurationOptionAccessor.InformaticsGatewayServerPort);
        }

        [Fact]
        public void InformaticsGatewayServerUri_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"InformaticsGatewayServerEndpoint\": \"http://localhost:5000\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal(new Uri("http://localhost:5000"), configurationOptionAccessor.InformaticsGatewayServerUri);
        }

        [Fact]
        public void Runner_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"Runner\": \"Docker\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);

            Assert.Equal(Runner.Docker, configurationOptionAccessor.Runner);
        }

        [Fact]
        public void Runner_Set_UpdatesValue()
        {
            var fileSystem = SetupFileSystem("{\"Cli\": {\"Runner\": \"Docker\"}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);
            configurationOptionAccessor.Runner = Runner.Kubernetes;
            Assert.Equal(Runner.Kubernetes, configurationOptionAccessor.Runner);
        }

        [Fact]
        public void TempStoragePath_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem("{\"InformaticsGateway\": {\"storage\": {\"localTemporaryStoragePath\": \"/payloads\"}}}");
            var configurationOptionAccessor = new ConfigurationOptionAccessor(fileSystem);
            Assert.Equal("/payloads", configurationOptionAccessor.TempStoragePath);
        }

        private IFileSystem SetupFileSystem(string config)
        {
            Guard.Against.NullOrWhiteSpace(config, nameof(config));

            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {Common.ConfigFilePath, new MockFileData(config) }
            });
        }
    }
}
