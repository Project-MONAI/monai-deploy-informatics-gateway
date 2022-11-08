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

        public static Configurations Instance { get; private set; }

        public InformaticsGatewaySettings InformaticsGatewayOptions { get; private set; }
        public Dictionary<string, StudySpec> StudySpecs { get; private set; }
        public OrthancSettings OrthancOptions { get; private set; }

        private Configurations(ISpecFlowOutputHelper outputHelper)
        {
            OrthancOptions = new OrthancSettings();
            StudySpecs = LoadStudySpecs() ?? throw new NullReferenceException("study.json not found or empty.");

            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.ext.json", optional: false, reloadOnChange: false)
                .Build();

            LoadConfiguration(outputHelper);
        }

        internal static void Initialize(ISpecFlowOutputHelper outputHelper)
        {
            Instance = new Configurations(outputHelper);
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

        private void LoadConfiguration(ISpecFlowOutputHelper outputHelper)
        {
            InformaticsGatewayOptions = new InformaticsGatewaySettings();

            _config.GetSection(nameof(OrthancSettings)).Bind(OrthancOptions);

            var hostIp = Environment.GetEnvironmentVariable("HOST_IP");
            if (hostIp is not null)
            {
                if (OrthancOptions.Host == "$HOST_IP")
                {
                    OrthancOptions.Host = hostIp;
                }
                outputHelper.WriteLine("Orthanc Host/IP = {0}", OrthancOptions.Host);
                if (OrthancOptions.DicomWebRoot.Contains("$HOST_IP"))
                {
                    OrthancOptions.DicomWebRoot = OrthancOptions.DicomWebRoot.Replace("$HOST_IP", hostIp);
                }
                outputHelper.WriteLine("Orthanc DICOM web endpoint = {0}", OrthancOptions.DicomWebRoot);
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

    public class InformaticsGatewaySettings
    {
        /// <summary>
        /// Gets or set host name or IP address of the Informatics Gateway.
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// Gets or sets the RESTful API port of the Informatics Gateway.
        /// </summary>
        public int ApiPort { get; set; } = 5000;

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
}
