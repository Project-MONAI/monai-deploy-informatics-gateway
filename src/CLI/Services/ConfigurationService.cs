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
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public enum Runner
    {
        Docker,
        Kubernetes,
        Helm,
    }

    public interface IConfigurationService
    {
        string TempStoragePath { get; }
        string LogStoragePath { get; }
        string HostDataStorageMount { get; }
        string HostDatabaseStorageMount { get; }
        string HostLogsStorageMount { get; }
        string InformaticsGatewayServer { get; set; }
        Uri InformaticsGatewayServerUri { get; }
        string WorkloadManagerRestEndpoint { get; set; }
        string WorkloadManagerGrpcEndpoint { get; set; }
        int DicomListeningPort { get; set; }
        int InformaticsGatewayServerPort { get; }
        Runner Runner { get; set; }
        string DockerImagePrefix { get; }

        bool IsConfigExists { get; }
        bool IsInitialized { get; }
        Task Initialize();
        void CreateConfigDirectoryIfNotExist();

    }

    public class ConfigurationService : IConfigurationService
    {
        private static readonly Object SyncLock = new object();
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IFileSystem _fileSystem;

        public bool IsInitialized => _fileSystem.Directory.Exists(Common.MigDirectory) &&
                    IsConfigExists;

        public bool IsConfigExists => _fileSystem.File.Exists(Common.ConfigFilePath);

        public ConfigurationService(ILogger<ConfigurationService> logger, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void CreateConfigDirectoryIfNotExist()
        {
            if (!_fileSystem.Directory.Exists(Common.MigDirectory))
            {
                _fileSystem.Directory.CreateDirectory(Common.MigDirectory);
            }
        }


        public async Task Initialize()
        {
            this._logger.Log(LogLevel.Debug, $"Reading default application configurations...");
            using var stream = this.GetType().Assembly.GetManifestResourceStream(Common.AppSettingsResourceName);

            if (stream is null)
            {
                _logger.Log(LogLevel.Debug, $"Available manifest names {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
                throw new Exception($"Default configuration '{Common.AppSettingsResourceName}' could not be loaded.");
            }
            CreateConfigDirectoryIfNotExist();

            this._logger.Log(LogLevel.Information, $"Saving appsettings.json to {Common.ConfigFilePath}...");
            using var fileStream = _fileSystem.File.Create(Common.ConfigFilePath);
            await stream.CopyToAsync(fileStream);
            this._logger.Log(LogLevel.Information, $"{Common.ConfigFilePath} updated successfully.");
        }
        public string InformaticsGatewayServer
        {
            get
            {
                return GetValueFromJsonPath<string>("Cli.InformaticsGatewayServerEndpoint");
            }
            set
            {
                Guard.Against.MalformUri(value, nameof(InformaticsGatewayServer));
                var jObject = ReadConfigurationFile();
                jObject["Cli"]["InformaticsGatewayServerEndpoint"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public Uri InformaticsGatewayServerUri
        {
            get
            {
                return new Uri(InformaticsGatewayServer);
            }
        }

        public int InformaticsGatewayServerPort
        {
            get
            {
                return InformaticsGatewayServerUri.Port;
            }
        }

        public string WorkloadManagerRestEndpoint
        {
            get
            {
                return GetValueFromJsonPath<string>("InformaticsGateway.workloadManager.restEndpoint");
            }
            set
            {
                Guard.Against.MalformUri(value, nameof(InformaticsGatewayServer));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["workloadManager"]["restEndpoint"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public string WorkloadManagerGrpcEndpoint
        {
            get
            {
                return GetValueFromJsonPath<string>("InformaticsGateway.workloadManager.grpcEndpoint");
            }
            set
            {
                Guard.Against.MalformUri(value, nameof(InformaticsGatewayServer));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["workloadManager"]["grpcEndpoint"] = value;
                SaveConfigurationFile(jObject);
            }
        }
        public string DockerImagePrefix
        {
            get
            {
                return GetValueFromJsonPath<string>("Cli.DockerImagePrefix");
            }
        }

        public int DicomListeningPort
        {
            get
            {
                return GetValueFromJsonPath<int>("InformaticsGateway.dicom.scp.port");
            }
            set
            {
                Guard.Against.OutOfRangePort(value, nameof(InformaticsGatewayServer));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["dicom"]["scp"]["port"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public Runner Runner
        {
            get
            {
                var runner = GetValueFromJsonPath<string>("Cli.Runner");
                return (Runner)Enum.Parse(typeof(Runner), runner);
            }
            set
            {
                var jObject = ReadConfigurationFile();
                jObject["Cli"]["Runner"] = value.ToString();
                SaveConfigurationFile(jObject);
            }
        }

        public string HostDataStorageMount
        {
            get
            {
                var path = GetValueFromJsonPath<string>("Cli.HostDataStorageMount");
                if (path.StartsWith("~/"))
                {
                    path = path.Replace("~/", $"{Common.HomeDir}/");
                }
                return path;
            }
        }

        public string HostDatabaseStorageMount
        {
            get
            {
                var path = GetValueFromJsonPath<string>("Cli.HostDatabaseStorageMount");
                if (path.StartsWith("~/"))
                {
                    path = path.Replace("~/", $"{Common.HomeDir}/");
                }
                return path;
            }
        }

        public string HostLogsStorageMount
        {
            get
            {
                var path = GetValueFromJsonPath<string>("Cli.HostLogsStorageMount");
                if (path.StartsWith("~/"))
                {
                    path = path.Replace("~/", $"{Common.HomeDir}/");
                }
                return path;
            }
        }

        public string TempStoragePath
        {
            get
            {
                return GetValueFromJsonPath<string>("InformaticsGateway.storage.temporary");
            }
        }

        public string LogStoragePath
        {
            get
            {
                var logPath = GetValueFromJsonPath<string>("Logging.File.BasePath");
                if(logPath.StartsWith("/"))
                {
                    return logPath;
                }
                return _fileSystem.Path.Combine(Common.ContainerApplicationRootPath, logPath);
            }
        }

        private T GetValueFromJsonPath<T>(string jsonPath)
        {
            return ReadConfigurationFile().SelectToken(jsonPath).Value<T>();
        }

        private JObject ReadConfigurationFile()
        {
            lock (SyncLock)
            {
                return JObject.Parse(_fileSystem.File.ReadAllText(Common.ConfigFilePath));
            }
        }

        private void SaveConfigurationFile(JObject jObject)
        {
            lock (SyncLock)
            {
                using (var file = _fileSystem.File.CreateText(Common.ConfigFilePath))
                using (var writer = new JsonTextWriter(file))
                {
                    writer.Formatting = Formatting.Indented;
                    jObject.WriteTo(writer, new Newtonsoft.Json.Converters.StringEnumConverter());
                }
            }
        }
    }
}
