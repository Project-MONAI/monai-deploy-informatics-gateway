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

using System.Net;
using System.Net.Http.Headers;
using Ardalis.GuardClauses;
using BoDi;
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

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomDimseScuServicesStepDefinitions
    {
        internal static readonly TimeSpan DicomScpWaitTimeSpan = TimeSpan.FromMinutes(3);
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly Configurations _configuration;
        private readonly DicomScp _dicomServer;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly IDataClient _dataSink;
        private readonly RabbitMQMessagePublisherService _messagePublisher;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly IDatabaseDataProvider _databaseDataProvider;
        private readonly DataProvider _dataProvider;
        private string _dicomDestination;

        public DicomDimseScuServicesStepDefinitions(ObjectContainer objectContainer, Configurations configuration)
        {
            if (objectContainer is null)
            {
                throw new ArgumentNullException(nameof(objectContainer));
            }
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _dicomServer = objectContainer.Resolve<DicomScp>("DicomScp");
            _messagePublisher = objectContainer.Resolve<RabbitMQMessagePublisherService>("MessagingPublisher");
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("ExportCompleteSubscriber");
            _databaseDataProvider = objectContainer.Resolve<IDatabaseDataProvider>("Database");
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _informaticsGatewayClient = objectContainer.Resolve<InformaticsGatewayClient>("InformaticsGatewayClient");
            _dataSink = objectContainer.Resolve<IDataClient>("MinioClient");
        }

        [Given(@"a DICOM destination registered with Informatics Gateway")]
        public async Task GivenADicomScpWithAET()
        {
            DestinationApplicationEntity destination;
            try
            {
                destination = await _informaticsGatewayClient.DicomDestinations.Create(new DestinationApplicationEntity
                {
                    Name = _dicomServer.FeatureScpAeTitle,
                    AeTitle = _dicomServer.FeatureScpAeTitle,
                    HostIp = _configuration.InformaticsGatewayOptions.Host,
                    Port = _dicomServer.FeatureScpPort
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict && ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    destination = await _informaticsGatewayClient.DicomDestinations.GetAeTitle(_dicomServer.FeatureScpAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
            _dicomDestination = destination.Name;
        }

        [Given(@"an ACR request in the database")]
        public async Task GivenDICOMInstances()
        {
            _dicomDestination = await _databaseDataProvider.InjectAcrRequest().ConfigureAwait(false);
        }

        [Given(@"(.*) (.*) studies for export")]
        public async Task GivenDICOMInstances(int studyCount, string modality)
        {
            Guard.Against.NegativeOrZero(studyCount);
            Guard.Against.NullOrWhiteSpace(modality);

            _dataProvider.GenerateDicomData(modality, studyCount);
            await _dataSink.SendAsync(_dataProvider);
            _dataProvider.ReplaceGeneratedDicomDataWithHashes();
        }

        [When(@"a export request is sent for '([^']*)'")]
        public void WhenAExportRequestIsReceivedDesignatedFor(string routingKey)
        {
            Guard.Against.NullOrWhiteSpace(routingKey);

            var exportRequestEvent = new ExportRequestEvent
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Destinations = new[] { _dicomDestination },
                ExportTaskId = Guid.NewGuid().ToString(),
                Files = _dataProvider.DicomSpecs.FileHashes.Keys.ToList(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };

            var message = new JsonMessage<ExportRequestEvent>(
                exportRequestEvent,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                exportRequestEvent.CorrelationId,
                string.Empty);

            _receivedMessages.ClearMessages();
            _messagePublisher.Publish(routingKey, message.ToMessage());
        }

        [Then(@"Informatics Gateway exports the studies to the DICOM SCP")]
        public async Task ThenExportTheInstancesToTheDicomScp()
        {
            (await _receivedMessages.WaitforAsync(1, DicomScpWaitTimeSpan)).Should().BeTrue();

            foreach (var key in _dataProvider.DicomSpecs.FileHashes.Keys)
            {
                (await Extensions.WaitUntil(() => _dicomServer.Instances.ContainsKey(key), DicomScpWaitTimeSpan)).Should().BeTrue("{0} should be received", key);
                _dicomServer.Instances.Should().ContainKey(key).WhoseValue.Equals(_dataProvider.DicomSpecs.FileHashes[key]);
            }
        }

        [Then(@"Informatics Gateway exports the studies to Orthanc")]
        public async Task ThenExportTheInstancesToOrthanc()
        {
            (await _receivedMessages.WaitforAsync(1, DicomScpWaitTimeSpan)).Should().BeTrue();
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
                     await foreach (var dicomFile in dicomWebClient.Wado.Retrieve(_dataProvider.DicomSpecs.StudyInstanceUids[0]))
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
                 return expected.Count == _dataProvider.DicomSpecs.FileHashes.Count;
             }, DicomScpWaitTimeSpan, 1000);

            result.Should().NotBeNull().And.HaveCount(_dataProvider.DicomSpecs.FileHashes.Count);
        }
    }
}
