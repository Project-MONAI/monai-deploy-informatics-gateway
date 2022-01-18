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
using Monai.Deploy.InformaticsGateway.Client.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IConfigurationOptionAccessor
    {
        /// <summary>
        /// Gets or sets the DICOM SCP listening port from appsettings.json.
        /// </summary>
        int DicomListeningPort { get; set; }

        /// <summary>
        /// Gets or sets the Docker image prefix from appsettings.json.
        /// This is used to query the Informatics Gateway Docker containers that are installed.
        /// </summary>
        string DockerImagePrefix { get; }

        /// <summary>
        /// Gets the database storage location on the host system from appsettings.json.
        /// </summary>
        string HostDatabaseStorageMount { get; }

        /// <summary>
        /// Gets the temprary data storage location on the host system from appsettings.json.
        /// </summary>
        string HostDataStorageMount { get; }

        /// <summary>
        /// Gets the logs storages location on the host system from appsettings.json.
        /// </summary>
        string HostLogsStorageMount { get; }

        /// <summary>
        /// Gets or sets the endpoint of the Informatics Gateway.
        /// </summary>
        string InformaticsGatewayServerEndpoint { get; set; }

        /// <summary>
        /// Gets the port number of the Informatics Gateway server.
        /// </summary>
        int InformaticsGatewayServerPort { get; }

        /// <summary>
        /// Gets the endpoint of the Informatics Gateway as Uri object.
        /// </summary>
        Uri InformaticsGatewayServerUri { get; }

        /// <summary>
        /// Gets the log storage path from appsettings.json.
        /// </summary>
        string LogStoragePath { get; }

        /// <summary>
        /// Gets or set the type of container runner from appsettings.json.
        /// </summary>
        Runner Runner { get; set; }

        /// <summary>
        /// Gets the temporary storage path from appsettings.json.
        /// </summary>
        string TempStoragePath { get; }

        /// <summary>
        /// Gets or sets the endpoint to the gRPC API of the Workload Manager from appsettings.json.
        /// </summary>
        string WorkloadManagerGrpcEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the endpoint to the RESTful API of the Workload Manager from appsettings.json.
        /// </summary>
        string WorkloadManagerRestEndpoint { get; set; }
    }

    public class ConfigurationOptionAccessor : IConfigurationOptionAccessor
    {
        private static readonly Object SyncLock = new object();
        private readonly IFileSystem _fileSystem;

        public ConfigurationOptionAccessor(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public int DicomListeningPort
        {
            get
            {
                return GetValueFromJsonPath<int>("InformaticsGateway.dicom.scp.port");
            }
            set
            {
                Guard.Against.OutOfRangePort(value, nameof(InformaticsGatewayServerEndpoint));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["dicom"]["scp"]["port"] = value;
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

        public string InformaticsGatewayServerEndpoint
        {
            get
            {
                return GetValueFromJsonPath<string>("Cli.InformaticsGatewayServerEndpoint");
            }
            set
            {
                Guard.Against.MalformUri(value, nameof(InformaticsGatewayServerEndpoint));
                var jObject = ReadConfigurationFile();
                jObject["Cli"]["InformaticsGatewayServerEndpoint"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public int InformaticsGatewayServerPort
        {
            get
            {
                return InformaticsGatewayServerUri.Port;
            }
        }

        public Uri InformaticsGatewayServerUri
        {
            get
            {
                return new Uri(InformaticsGatewayServerEndpoint);
            }
        }

        public string LogStoragePath
        {
            get
            {
                var logPath = GetValueFromJsonPath<string>("Logging.File.BasePath");
                if (logPath.StartsWith("/"))
                {
                    return logPath;
                }
                return _fileSystem.Path.Combine(Common.ContainerApplicationRootPath, logPath);
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

        public string TempStoragePath
        {
            get
            {
                return GetValueFromJsonPath<string>("InformaticsGateway.storage.temporary");
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
                Guard.Against.MalformUri(value, nameof(InformaticsGatewayServerEndpoint));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["workloadManager"]["grpcEndpoint"] = value;
                SaveConfigurationFile(jObject);
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
                Guard.Against.MalformUri(value, nameof(InformaticsGatewayServerEndpoint));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["workloadManager"]["restEndpoint"] = value;
                SaveConfigurationFile(jObject);
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
