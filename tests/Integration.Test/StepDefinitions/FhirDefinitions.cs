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

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using Ardalis.GuardClauses;
using Minio;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class FhirDefinitions
    {
        internal enum FileFormat
        { Xml, Json };

        internal static readonly TimeSpan WaitTimeSpan = TimeSpan.FromMinutes(2);
        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly RabbitMqHooks _rabbitMqHooks;
        private readonly Dictionary<string, string> _input;
        private readonly Dictionary<string, string> _output;
        private FileFormat _fileFormat;
        private string _mediaType;

        public FhirDefinitions(
            FeatureContext featureContext,
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            RabbitMqHooks rabbitMqHooks)
        {
            _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));

            _input = new Dictionary<string, string>();
            _output = new Dictionary<string, string>();
        }

        [Given(@"FHIR message (.*) in (.*)")]
        public async Task GivenHl7MessagesInVersionX(string version, string format)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));
            Guard.Against.NullOrWhiteSpace(format, nameof(format));

            var files = Directory.GetFiles($"data/fhir/{version}", $"*.{format.ToLowerInvariant()}");

            _fileFormat = format == "XML" ? FileFormat.Xml : FileFormat.Json;
            _mediaType = _fileFormat == FileFormat.Xml ? "application/fhir+xml" : "application/fhir+json";

            foreach (var file in files)
            {
                _outputHelper.WriteLine($"Adding file {file}");
                _input[file] = await File.ReadAllTextAsync(file);
            }
            _rabbitMqHooks.SetupMessageHandle(files.Length);
        }

        [When(@"the FHIR messages are sent to Informatics Gateway")]
        public async Task WhenTheMessagesAreSentToInformaticsGateway()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{_configuration.InformaticsGatewayOptions.ApiEndpoint}/fhir/");
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(_mediaType));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", _mediaType);

            foreach (var file in _input.Keys)
            {
                var path = Path.GetFileNameWithoutExtension(file);
                _outputHelper.WriteLine($"Sending file {file} to /fhir/{path}...");
                var httpContent = new StringContent(_input[file], Encoding.UTF8, _mediaType);
                var response = await httpClient.PostAsync(path, httpContent);
                response.EnsureSuccessStatusCode();
            }
        }

        [Then(@"workflow requests are sent to message broker")]
        public void ThenWorkflowRequestAreSentToMessageBroker()
        {
            _rabbitMqHooks.MessageWaitHandle.Wait(WaitTimeSpan).Should().BeTrue();
        }

        [Then(@"FHIR resources are uploaded to storage service")]
        public async Task ThenFhirResourcesAreUploadedToStorageService()
        {
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            messages.Should().NotBeNullOrEmpty().And.HaveCount(_input.Count);

            var minioClient = new MinioClient()
                .WithEndpoint(_configuration.StorageServiceOptions.Endpoint)
                .WithCredentials(_configuration.StorageServiceOptions.AccessKey, _configuration.StorageServiceOptions.AccessToken);

            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be(1);

                foreach (var file in request.Payload)
                {
                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(request.Bucket)
                        .WithObject($"{request.PayloadId}/{file.Path}")
                        .WithCallbackStream((stream) =>
                        {
                            using var memoryStream = new MemoryStream();
                            stream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            var data = Encoding.UTF8.GetString(memoryStream.ToArray());
                            data.Should().NotBeNullOrWhiteSpace();

                            var incomingFilename = Path.GetFileName(file.Path);
                            var storedFileKey = _input.Keys.FirstOrDefault(p => p.EndsWith(incomingFilename));
                            storedFileKey.Should().NotBeNull();

                            _outputHelper.WriteLine($"Validating file {storedFileKey}...");
                            if (incomingFilename.EndsWith(".json", true, CultureInfo.InvariantCulture))
                            {
                                ValidateJson(_input[storedFileKey], data);
                            }
                            else
                            {
                                ValidateXml(_input[storedFileKey], data);
                            }
                        });
                    await minioClient.GetObjectAsync(getObjectArgs);
                }
            }
        }

        private void ValidateXml(string expected, string actual)
        {
            expected = FormatXml(expected);
            expected.Should().Be(actual);
        }

        private string FormatXml(string xml)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
            {
                xmlDocument.Save(xmlWriter);
            }
            return sb.ToString();
        }

        private void ValidateJson(string expected, string actual)
        {
            expected = FormatJson(expected);
            expected.Should().Be(actual);
        }

        private string FormatJson(string expected)
        {
            var jsonDoc = JsonNode.Parse(expected);
            return jsonDoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
