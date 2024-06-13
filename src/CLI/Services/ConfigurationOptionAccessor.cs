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
using System.IO.Abstractions;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IConfigurationOptionAccessor
    {
        /// <summary>
        /// Gets or sets the DICOM SCP listening port from appsettings.json.
        /// </summary>
        int DicomListeningPort { get; set; }

        /// <summary>
        /// Gets or sets the ExternalApp DICOM SCP listening port from appsettings.json.
        /// </summary>
        int ExternalAppDicomListeningPort { get; set; }

        /// <summary>
        /// Gets or sets the HL7 listening port from appsettings.json.
        /// </summary>
        int Hl7ListeningPort { get; set; }

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
        /// Gets the temprary data storage location on the host system from appsettings.json.
        /// </summary>
        string HostPlugInsStorageMount { get; }

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
        Uri? InformaticsGatewayServerUri { get; }

        /// <summary>
        /// Gets or set the type of container runner from appsettings.json.
        /// </summary>
        Runner Runner { get; set; }

        /// <summary>
        /// Gets the temporary storage path from appsettings.json.
        /// </summary>
        string TempStoragePath { get; }
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
    public class ConfigurationOptionAccessor : IConfigurationOptionAccessor
    {
        private static readonly Object SyncLock = new();
        private readonly IFileSystem _fileSystem;

        public ConfigurationOptionAccessor(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public int DicomListeningPort
        {
            get
            {
                return GetValueFromJsonPath<int>("InformaticsGateway.dicom.scp.port");
            }
            set
            {
                Guard.Against.OutOfRangePort(value, nameof(DicomListeningPort));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["dicom"]["scp"]["port"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public int ExternalAppDicomListeningPort
        {
            get
            {
                return GetValueFromJsonPath<int>("InformaticsGateway.dicom.scp.externalAppPort");
            }
            set
            {
                Guard.Against.OutOfRangePort(value, nameof(ExternalAppDicomListeningPort));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["dicom"]["scp"]["externalAppPort"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public int Hl7ListeningPort
        {
            get
            {
                return GetValueFromJsonPath<int>("InformaticsGateway.hl7.port");
            }
            set
            {
                Guard.Against.OutOfRangePort(value, nameof(Hl7ListeningPort));
                var jObject = ReadConfigurationFile();
                jObject["InformaticsGateway"]["hl7"]["port"] = value;
                SaveConfigurationFile(jObject);
            }
        }

        public string DockerImagePrefix
        {
            get
            {
                return GetValueFromJsonPath<string>("Cli.DockerImagePrefix") ?? string.Empty;
            }
        }

        public string HostDatabaseStorageMount
        {
            get
            {
                var path = GetValueFromJsonPath<string>("Cli.HostDatabaseStorageMount") ?? string.Empty;
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
                var path = GetValueFromJsonPath<string>("Cli.HostDataStorageMount") ?? string.Empty;
                if (path.StartsWith("~/"))
                {
                    path = path.Replace("~/", $"{Common.HomeDir}/");
                }
                return path;
            }
        }

        public string HostPlugInsStorageMount
        {
            get
            {
                var path = GetValueFromJsonPath<string>("Cli.HostPlugInsStorageMount") ?? string.Empty;
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
                var path = GetValueFromJsonPath<string>("Cli.HostLogsStorageMount") ?? string.Empty;
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
                return GetValueFromJsonPath<string>("Cli.InformaticsGatewayServerEndpoint") ?? string.Empty;
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
                return InformaticsGatewayServerUri?.Port ?? 0;
            }
        }

        public Uri? InformaticsGatewayServerUri
        {
            get
            {
                if (InformaticsGatewayServerEndpoint is not null)
                {
                    return new Uri(InformaticsGatewayServerEndpoint);
                }

                return null;
            }
        }

        public Runner Runner
        {
            get
            {
                var runner = GetValueFromJsonPath<string>("Cli.Runner");
                if (runner is not null)
                {
                    return (Runner)Enum.Parse(typeof(Runner), runner);
                }
                return Runner.Unknown;
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
                return GetValueFromJsonPath<string>("InformaticsGateway.storage.localTemporaryStoragePath") ?? string.Empty;
            }
        }

        private T? GetValueFromJsonPath<T>(string jsonPath)
        {
            var token = ReadConfigurationFile().SelectToken(jsonPath);

            if (token is not null)
                return token.Value<T>();

            return default;
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
                using var file = _fileSystem.File.CreateText(Common.ConfigFilePath);
                using var writer = new JsonTextWriter(file);
                writer.Formatting = Formatting.Indented;
                jObject.WriteTo(writer, new Newtonsoft.Json.Converters.StringEnumConverter());
            }
        }
    }
}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
