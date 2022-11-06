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
using System.Net;
using System.Net.Http.Headers;
using Ardalis.GuardClauses;
using BoDi;
using Minio;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Messaging.RabbitMQ;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomDimseScuServicesStepDefinitions
    {
        internal static readonly TimeSpan DicomScpWaitTimeSpan = TimeSpan.FromMinutes(2);
        internal static readonly TimeSpan DicomWebWaitTimeSpan = TimeSpan.FromMinutes(2);
        internal static readonly string KeyPatientId = "PATIENT_ID";
        internal static readonly string KeyDicomHashes = "DICOM_FILES";
        internal static readonly string KeyDestination = "EXPORT_DESTINATION";
        internal static readonly string KeyExportRequestMessage = "EXPORT_REQUEST-MESSAGE";
        internal static readonly string KeyFileSpecs = "FILE_SPECS";
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly RabbitMQMessagePublisherService _messagePublisher;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly IDatabaseDataProvider _databaseDataProvider;

        public DicomDimseScuServicesStepDefinitions(
            ObjectContainer objectContainer,
            FeatureContext featureContext,
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            DicomInstanceGenerator dicomInstanceGenerator,
            InformaticsGatewayClient informaticsGatewayClient)
        {
            if (objectContainer is null)
            {
                throw new ArgumentNullException(nameof(objectContainer));
            }
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomInstanceGenerator = dicomInstanceGenerator ?? throw new ArgumentNullException(nameof(dicomInstanceGenerator));
            _informaticsGatewayClient = informaticsGatewayClient ?? throw new ArgumentNullException(nameof(informaticsGatewayClient));
            _informaticsGatewayClient.ConfigureServiceUris(new Uri(_configuration.InformaticsGatewayOptions.ApiEndpoint));


            _messagePublisher = objectContainer.Resolve<RabbitMQMessagePublisherService>("MessagingPublisher");
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("ExportCompleteSubscriber");
            _databaseDataProvider = objectContainer.Resolve<IDatabaseDataProvider>("Database");
        }

        [Given(@"a DICOM destination registered with Informatics Gateway")]
        public async Task GivenADicomScpWithAET()
        {
            DestinationApplicationEntity destination;
            try
            {
                destination = await _informaticsGatewayClient.DicomDestinations.Create(new DestinationApplicationEntity
                {
                    Name = ScpHooks.FeatureScpAeTitle,
                    AeTitle = ScpHooks.FeatureScpAeTitle,
                    HostIp = _configuration.InformaticsGatewayOptions.Host,
                    Port = ScpHooks.FeatureScpPort
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict && ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    destination = await _informaticsGatewayClient.DicomDestinations.GetAeTitle(ScpHooks.FeatureScpAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
            _scenarioContext[KeyDestination] = destination.Name;
        }

        [Given(@"an ACR request in the database")]
        public async Task GivenDICOMInstances()
        {
            _scenarioContext[KeyDestination] = await _databaseDataProvider.InjectAcrRequest().ConfigureAwait(false);
        }

        [Given(@"(.*) (.*) studies for export")]
        public async Task GivenDICOMInstances(int studyCount, string modality)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));

            _outputHelper.WriteLine($"Generating {studyCount} {modality} study");
            _configuration.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configuration.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, modality, studySpec);

            var hashes = new Dictionary<string, string>();

            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount} studies, {fileSpecs.SeriesPerStudyCount} series/study, {fileSpecs.InstancePerSeries} instances/series, {fileSpecs.FileCount} files total");

            var minioClient = new MinioClient()
                .WithEndpoint(_informaticsGatewayConfiguration.Storage.Settings["endpoint"])
                .WithCredentials(_informaticsGatewayConfiguration.Storage.Settings["accessKey"], _informaticsGatewayConfiguration.Storage.Settings["accessToken"])
                .Build();

            _outputHelper.WriteLine($"Uploading {fileSpecs.FileCount} files to MinIO...");
            foreach (var file in fileSpecs.Files)
            {
                var filename = file.GenerateFileName();
                hashes.Add(filename, file.CalculateHash());

                var stream = new MemoryStream();
                await file.SaveAsync(stream);
                stream.Position = 0;
                var puObjectArgs = new PutObjectArgs();
                puObjectArgs.WithBucket(_informaticsGatewayConfiguration.Storage.StorageServiceBucketName)
                    .WithObject(filename)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length);
                await minioClient.PutObjectAsync(puObjectArgs);
            }
            _scenarioContext[KeyDicomHashes] = hashes;
            _scenarioContext[KeyPatientId] = patientId;
            _scenarioContext[KeyFileSpecs] = fileSpecs;
        }

        [When(@"a export request is sent for '([^']*)'")]
        public void WhenAExportRequestIsReceivedDesignatedFor(string routingKey)
        {
            Guard.Against.NullOrWhiteSpace(routingKey, nameof(routingKey));

            var dicomHashes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;

            var destination = _scenarioContext[KeyDestination].ToString();

            var exportRequestEvent = new ExportRequestEvent
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Destinations = new[] { destination },
                ExportTaskId = Guid.NewGuid().ToString(),
                Files = dicomHashes.Keys.ToList(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };

            var message = new JsonMessage<ExportRequestEvent>(
                exportRequestEvent,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                exportRequestEvent.CorrelationId,
                string.Empty);

            _receivedMessages.SetupMessageHandle(1);
            _messagePublisher.Publish(routingKey, message.ToMessage());
            _scenarioContext[KeyExportRequestMessage] = exportRequestEvent;
        }

        [Then(@"Informatics Gateway exports the studies to the DICOM SCP")]
        public async Task ThenExportTheInstancesToTheDicomScp()
        {
            _receivedMessages.MessageWaitHandle.Wait(DicomScpWaitTimeSpan).Should().BeTrue();
            var data = _featureContext[ScpHooks.KeyServerData] as ServerData;
            var dicomHashes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;

            foreach (var key in dicomHashes.Keys)
            {
                (await Extensions.WaitUntil(() => data.Instances.ContainsKey(key), DicomScpWaitTimeSpan)).Should().BeTrue("{0} should be received", key);
                data.Instances.Should().ContainKey(key).WhoseValue.Equals(dicomHashes[key]);
            }
        }

        [Then(@"Informatics Gateway exports the studies to Orthanc")]
        public async Task ThenExportTheInstancesToOrthanc()
        {
            _receivedMessages.MessageWaitHandle.Wait(DicomScpWaitTimeSpan).Should().BeTrue();
            var dicomHashes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;
            var fileSpecs = _scenarioContext[KeyFileSpecs] as DicomInstanceGenerator.StudyGenerationSpecs;
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClient(httpClient, null);
            dicomWebClient.ConfigureServiceUris(new Uri(_configuration.OrthancOptions.DicomWebRoot));
            dicomWebClient.ConfigureAuthentication(new AuthenticationHeaderValue("Basic", _configuration.OrthancOptions.GetBase64EncodedAuthHeader()));
            var result = await Extensions.WaitUntilDataIsReady<Dictionary<string, string>>(async () =>
             {
                 var actualHashes = new Dictionary<string, string>();
                 try
                 {
                     var instanceFound = 0;
                     await foreach (var dicomFile in dicomWebClient.Wado.Retrieve(fileSpecs.StudyInstanceUids[0]))
                     {
                         var key = dicomFile.GenerateFileName();
                         var hash = dicomFile.CalculateHash();
                         actualHashes.Add(key, hash);
                         ++instanceFound;
                     }
                 }
                 catch
                 {
                     // noop
                 }
                 return actualHashes;
             }, (Dictionary<string, string> expected) =>
             {
                 return expected.Count == dicomHashes.Count;
             }, DicomWebWaitTimeSpan, 1000);

            result.Should().NotBeNull().And.HaveCount(dicomHashes.Count);
        }
    }
}
