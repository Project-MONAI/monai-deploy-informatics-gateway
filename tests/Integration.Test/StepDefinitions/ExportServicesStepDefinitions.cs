// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Ardalis.GuardClauses;
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
        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly RabbitMqHooks _rabbitMqHooks;

        public DicomDimseScuServicesStepDefinitions(
            FeatureContext featureContext,
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            DicomInstanceGenerator dicomInstanceGenerator,
            InformaticsGatewayClient informaticsGatewayClient,
            RabbitMqHooks rabbitMqHooks)
        {
            _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomInstanceGenerator = dicomInstanceGenerator ?? throw new ArgumentNullException(nameof(dicomInstanceGenerator));
            _informaticsGatewayClient = informaticsGatewayClient ?? throw new ArgumentNullException(nameof(informaticsGatewayClient));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));
            _informaticsGatewayClient.ConfigureServiceUris(new Uri(_configuration.InformaticsGatewayOptions.ApiEndpoint));
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
                    HostIp = _configuration.TestRunnerOptions.HostIp,
                    Port = ScpHooks.FeatureScpPort
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.BadRequest && ex.ProblemDetails.Detail.Contains("already exists"))
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
                .WithEndpoint(_configuration.StorageServiceOptions.Endpoint)
                .WithCredentials(_configuration.StorageServiceOptions.AccessKey, _configuration.StorageServiceOptions.AccessToken);

            _outputHelper.WriteLine($"Uploading {fileSpecs.FileCount} files to MinIO...");
            foreach (var file in fileSpecs.Files)
            {
                var filename = file.GenerateFileName();
                hashes.Add(filename, file.CalculateHash());

                var stream = new MemoryStream();
                await file.SaveAsync(stream);
                stream.Position = 0;
                var puObjectArgs = new PutObjectArgs();
                puObjectArgs.WithBucket(_configuration.TestRunnerOptions.Bucket)
                    .WithFileName(filename)
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
                Destination = destination,
                ExportTaskId = Guid.NewGuid().ToString(),
                Files = dicomHashes.Keys.ToList(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowId = Guid.NewGuid().ToString(),
            };

            var message = new JsonMessage<ExportRequestEvent>(
                exportRequestEvent,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                exportRequestEvent.CorrelationId,
                string.Empty);

            _rabbitMqHooks.SetupMessageHandle(1);
            _rabbitMqHooks.Publish(routingKey, message.ToMessage());
            _scenarioContext[KeyExportRequestMessage] = exportRequestEvent;
        }

        [Then(@"Informatics Gateway exports the studies to the DICOM SCP")]
        public async Task ThenExportTheInstancesToTheDicomScp()
        {
            _rabbitMqHooks.MessageWaitHandle.Wait(DicomScpWaitTimeSpan).Should().BeTrue();
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
