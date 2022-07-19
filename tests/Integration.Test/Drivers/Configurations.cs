/*
 * Copyright 2022 MONAI Consortium
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
 
using System.Configuration;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    public sealed class Configurations
    {
        private readonly IConfiguration _config;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public TestRunnerSettings TestRunnerOptions { get; private set; }
        public InformaticsGatewaySettings InformaticsGatewayOptions { get; private set; }
        public MessageBrokerSettings MessageBrokerOptions { get; private set; }
        public Dictionary<string, StudySpec> StudySpecs { get; private set; }
        public StorageServiceSettings StorageServiceOptions { get; private set; }
        public OrthancSettings OrthancOptions { get; private set; }

        public Configurations(ISpecFlowOutputHelper outputHelper)
        {
            TestRunnerOptions = new TestRunnerSettings();
            InformaticsGatewayOptions = new InformaticsGatewaySettings();
            MessageBrokerOptions = new MessageBrokerSettings();
            StorageServiceOptions = new StorageServiceSettings();
            OrthancOptions = new OrthancSettings();
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            StudySpecs = LoadStudySpecs() ?? throw new NullReferenceException("study.json not found or empty.");

            outputHelper.WriteLine($"StudySpecs={JsonSerializer.Serialize(StudySpecs)}");

            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                .Build();

            LoadConfiguration();
        }

        private Dictionary<string, StudySpec> LoadStudySpecs()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var studyJsonPath = Path.Combine(assemblyPath ?? string.Empty, "study.json");

            if (!File.Exists(studyJsonPath))
            {
                throw new FileNotFoundException($"study.json not found in {studyJsonPath}");
            }

            var studyJson = File.ReadAllText(studyJsonPath);
            return JsonSerializer.Deserialize<Dictionary<string, StudySpec>>(studyJson);
        }

        private void LoadConfiguration()
        {
            _config.GetSection(nameof(TestRunnerSettings)).Bind(TestRunnerOptions);
            _config.GetSection(nameof(InformaticsGatewaySettings)).Bind(InformaticsGatewayOptions);
            _config.GetSection(nameof(MessageBrokerSettings)).Bind(MessageBrokerOptions);
            _config.GetSection(nameof(StorageServiceSettings)).Bind(StorageServiceOptions);
            _config.GetSection(nameof(OrthancSettings)).Bind(OrthancOptions);

            if (InformaticsGatewayOptions.TemporaryDataStore == "$DATA_PATH")
            {
                InformaticsGatewayOptions.TemporaryDataStore = Environment.GetEnvironmentVariable("DATA_PATH") ?? throw new ConfigurationErrorsException("Environment variable 'DATA_PATH' is undefined.");
            }
            _outputHelper.WriteLine("Informatics Gateway data path = {0}", InformaticsGatewayOptions.TemporaryDataStore);

            var hostIp = Environment.GetEnvironmentVariable("HOST_IP");
            if (hostIp is not null)
            {
                if (TestRunnerOptions.HostIp == "$HOST_IP")
                {
                    TestRunnerOptions.HostIp = hostIp;
                }
                _outputHelper.WriteLine("Test Runner Host/IP = {0}", TestRunnerOptions.HostIp);
                if (InformaticsGatewayOptions.Host == "$HOST_IP")
                {
                    InformaticsGatewayOptions.Host = hostIp;
                }
                _outputHelper.WriteLine("Informatics Gateway Host/IP = {0}", InformaticsGatewayOptions.Host);
                if (MessageBrokerOptions.Endpoint == "$HOST_IP")
                {
                    MessageBrokerOptions.Endpoint = hostIp;
                }
                _outputHelper.WriteLine("Message Broker Host/IP = {0}", MessageBrokerOptions.Endpoint);
                if (StorageServiceOptions.Host == "$HOST_IP")
                {
                    StorageServiceOptions.Host = hostIp;
                }
                _outputHelper.WriteLine("Storage Service Host/IP = {0}", StorageServiceOptions.Host);
                if (OrthancOptions.Host == "$HOST_IP")
                {
                    OrthancOptions.Host = hostIp;
                }
                _outputHelper.WriteLine("Orthanc Host/IP = {0}", OrthancOptions.Host);
                if (OrthancOptions.DicomWebRoot.Contains("$HOST_IP"))
                {
                    OrthancOptions.DicomWebRoot = OrthancOptions.DicomWebRoot.Replace("$HOST_IP", hostIp);
                }
                _outputHelper.WriteLine("Orthanc DICOM web endpoint = {0}", OrthancOptions.DicomWebRoot);
            }
        }
    }

    public class OrthancSettings
    {
        /// <summary>
        /// Gets or set host name or IP address the Orthanc instance.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the DIMSE port of the Orthanc server.
        /// </summary>
        public int DimsePort { get; set; }

        /// <summary>
        /// Gets or sets the root URI of the Orthanc DICOMweb service.
        /// </summary>
        public string DicomWebRoot { get; set; }

        /// <summary>
        /// Gets or sets the root URI of the Orthanc DICOMweb service used by IG.
        /// </summary>
        public string DicomWebRootInternal { get; set; }

        /// <summary>
        /// Gets or sets the username to access Orthanc.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password to access Orthanc.
        /// </summary>
        public string Password { get; set; }

        public string GetBase64EncodedAuthHeader()
        {
            var authToken = Encoding.ASCII.GetBytes($"{Username}:{Password}");
            return Convert.ToBase64String(authToken);
        }
    }

    public class StorageServiceSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string AccessToken { get; set; }
        public string AccessKey { get; set; }

        public string Endpoint => $"{Host}:{Port}";
    }

    public class TestRunnerSettings
    {
        /// <summary>
        /// Gets or sets the Host/IP Address used when createing a DICOM source.
        /// If not specified, the test runner would query and use first available IPv4 IP Address.
        /// </summary>
        /// <value></value>
        public string HostIp { get; set; }

        /// <summary>
        /// Gets or sets the name of the bucket test files are uploaded to.
        /// </summary>
        public string Bucket { get; set; }
    }

    public class InformaticsGatewaySettings
    {
        /// <summary>
        /// Gets or set the path where the temporary payloads are stored.
        /// </summary>
        public string TemporaryDataStore { get; set; }

        /// <summary>
        /// Gets or set host name or IP address of the Informatics Gateway.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the DIMSE port of the Informatics Gateway.
        /// </summary>
        public int DimsePort { get; set; }

        /// <summary>
        /// Gets or sets the RESTful API port of the Informatics Gateway.
        /// </summary>
        public int ApiPort { get; set; }

        /// <summary>
        /// Gets or sets the name of the bucket used by the storage service.
        /// </summary>
        public string StorageServiceBucketName { get; set; }

        /// <summary>
        /// Gets the API endpoint of the Informatics Gateway.
        /// </summary>
        public string ApiEndpoint
        {
            get
            {
                return $"http://{Host}:{ApiPort}";
            }
        }
    }

    /// <summary>
    /// Maps modality type specs from study.json
    /// </summary>
    public class StudySpec
    {
        private const int OneMiB = 1048576;
        public int SeriesMin { get; set; }
        public int SeriesMax { get; set; }
        public int InstanceMin { get; set; }
        public int InstanceMax { get; set; }
        public float SizeMin { get; set; }
        public float SizeMax { get; set; }

        public long SizeMinBytes
        {
            get { return (long)(SizeMin * OneMiB); }
        }

        public long SizeMaxBytes
        {
            get { return (long)(SizeMax * OneMiB); }
        }
    }

    public class MessageBrokerSettings
    {
        public string Endpoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string Exchange { get; set; }
    }
}
