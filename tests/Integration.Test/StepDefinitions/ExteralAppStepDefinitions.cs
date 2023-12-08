/*
 * Copyright 2022-2023 MONAI Consortium
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
using BoDi;
using FellowOakDicom;
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Messaging.RabbitMQ;
using Polly;
using Polly.Timeout;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class ExteralAppStepDefinitions
    {
        private static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan DicomScpWaitTimeSpan = TimeSpan.FromMinutes(20);
        private static readonly string MonaiAeTitle = "REMOTE-APPS";
        private static readonly string SourceAeTitle = "MIGTestHost";
        private static readonly DicomTag[] DicomTags = new[] { DicomTag.AccessionNumber, DicomTag.StudyDescription, DicomTag.SeriesDescription, DicomTag.PatientAddress, DicomTag.PatientAge, DicomTag.PatientName };
        private static readonly List<DicomTag> DefaultDicomTags = new() { DicomTag.PatientID, DicomTag.StudyInstanceUID, DicomTag.SeriesInstanceUID, DicomTag.SOPInstanceUID };

        private readonly ObjectContainer _objectContainer;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly IDataClient _dataSinkMinio;
        private readonly DicomScp _dicomServer;
        private readonly Configurations _configuration;
        private string _dicomDestination;
        private readonly DataProvider _dataProvider;
        private readonly RabbitMqConsumer _receivedExportCompletedMessages;
        private readonly RabbitMqConsumer _receivedWorkflowRequestMessages;
        private readonly RabbitMqConsumer _receivedArtifactRecievedMessages;
        private readonly RabbitMQMessagePublisherService _messagePublisher;
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private Dictionary<string, DicomFile> _originalDicomFiles;
        private ExternalAppRequestEvent _exportRequestEvent;
        private readonly Assertions _assertions;
        private readonly string _correlationId = Guid.NewGuid().ToString();
        private readonly string _exportTaskId = Guid.NewGuid().ToString();
        private readonly string _workflowInstanceId = Guid.NewGuid().ToString();

        public ExteralAppStepDefinitions(
            ObjectContainer objectContainer,
            Configurations configuration)
        {
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _informaticsGatewayClient = objectContainer.Resolve<InformaticsGatewayClient>("InformaticsGatewayClient");
            _dataSinkMinio = objectContainer.Resolve<IDataClient>("MinioClient");
            _dicomServer = objectContainer.Resolve<DicomScp>("DicomScp");
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _receivedExportCompletedMessages = objectContainer.Resolve<RabbitMqConsumer>("ExportCompleteSubscriber");
            _receivedWorkflowRequestMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _receivedArtifactRecievedMessages = objectContainer.Resolve<RabbitMqConsumer>("ArtifactRecievedSubscriber");
            _messagePublisher = objectContainer.Resolve<RabbitMQMessagePublisherService>("MessagingPublisher");
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");

            DefaultDicomTags.AddRange(DicomTags);
            _dicomServer.ClearFilesAndUseHashes = false; //we need to store actual files to send the data back to MIG
        }

        [Given(@"a externalApp study that is exported to the test host")]
        public async Task GivenAExternalAppStudyThatIsExportedToTheTestHost()
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

            // Generate a study with multiple series
            //_dataProvider.GenerateDicomData("MG", 1, 1);
            _dataProvider.GenerateDicomData("CT", 1);
            _dataProvider.InjectRandomData(DicomTags);
            _originalDicomFiles = new Dictionary<string, DicomFile>(_dataProvider.DicomSpecs.Files);

            await _dataSinkMinio.SendAsync(_dataProvider);

            // Emit a export request event
            _exportRequestEvent = new ExternalAppRequestEvent
            {
                CorrelationId = _correlationId,
                Targets = new List<DataOrigin> { new DataOrigin { Destination = destination.Name } },
                ExportTaskId = _exportTaskId,
                Files = _dataProvider.DicomSpecs.Files.Keys.ToList(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = _workflowInstanceId,
                DestinationFolder = "ThisIs/My/Output/Folder",
            };

            //_exportRequestEvent.PluginAssemblies.Add(typeof(DicomDeidentifier).AssemblyQualifiedName);

            var message = new JsonMessage<ExternalAppRequestEvent>(
                _exportRequestEvent,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                _exportRequestEvent.CorrelationId,
                string.Empty);

            _receivedExportCompletedMessages.ClearMessages();
            _receivedArtifactRecievedMessages.ClearMessages();
            await _messagePublisher.Publish("md.externalapp.request", message.ToMessage());
        }

        [When(@"the externalApp study is received and sent back to Informatics Gateway with (.*) message")]
        public async Task WhenTheExternalAppStudyIsReceivedAndSentBackToInformaticsGatewayWithMessage(int exportCount)
        {
            // setup DICOM Source
            try
            {
                await _informaticsGatewayClient.DicomSources.Create(new SourceApplicationEntity
                {
                    Name = SourceAeTitle,
                    AeTitle = SourceAeTitle,
                    HostIp = _configuration.InformaticsGatewayOptions.Host,
                }, CancellationToken.None);
                _dataProvider.Source = SourceAeTitle;
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    await _informaticsGatewayClient.DicomSources.GetAeTitle(SourceAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }

            // setup MONAI Deploy AET
            _dataProvider.StudyGrouping = "0020,000D";
            try
            {
                await _informaticsGatewayClient.MonaiScpAeTitle.Create(new MonaiApplicationEntity
                {
                    AeTitle = MonaiAeTitle,
                    Name = MonaiAeTitle,
                    Grouping = _dataProvider.StudyGrouping,
                    Timeout = 3,
                    PlugInAssemblies = new List<string>()
                }, CancellationToken.None);
                _dataProvider.Destination = MonaiAeTitle;
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    await _informaticsGatewayClient.MonaiScpAeTitle.GetAeTitle(MonaiAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }

            var timeoutPolicy = Policy.TimeoutAsync(140, TimeoutStrategy.Pessimistic);
            await timeoutPolicy
                .ExecuteAsync(
                    async () => { await SendRequest(exportCount); }
                  );

            // Clear workflow request messages
            _receivedWorkflowRequestMessages.ClearMessages();
            _receivedArtifactRecievedMessages.ClearMessages();

            _dataProvider.DimseRsponse.Should().Be(DicomStatus.Success);

            // Wait for workflow request events
            (await _receivedArtifactRecievedMessages.WaitforAsync(1, MessageWaitTimeSpan)).Should().BeTrue();
            _assertions.ShouldHaveCorrectNumberOfWorkflowRequestMessages(_dataProvider, DataService.DIMSE, _receivedArtifactRecievedMessages.Messages, 1);
        }

        [Then(@"ensure the original externalApp study and the received study are the same")]
        public async Task ThenEnsureTheOriginalExternalAppStudyAndTheReceivedStudyAreTheSame()
        {
            var workflowRequestEvent = _receivedArtifactRecievedMessages.Messages[0].ConvertTo<WorkflowRequestEvent>();
            _exportRequestEvent.CorrelationId.Should().Be(_receivedArtifactRecievedMessages.Messages[0].CorrelationId);
            _exportRequestEvent.CorrelationId.Should().Be(workflowRequestEvent.CorrelationId);
            _exportRequestEvent.WorkflowInstanceId.Should().Be(workflowRequestEvent.WorkflowInstanceId);
            _exportRequestEvent.ExportTaskId.Should().Be(workflowRequestEvent.TaskId);
            await _assertions.ShouldRestoreAllDicomMetaata(_receivedArtifactRecievedMessages.Messages, _originalDicomFiles, DefaultDicomTags.ToArray()).ConfigureAwait(false);
        }

        private async Task SendRequest(int exportCount = 1)
        {
            // Wait for export completed event
            (await _receivedExportCompletedMessages.WaitforAsync(exportCount, DicomScpWaitTimeSpan)).Should().BeTrue();

            foreach (var key in _dataProvider.DicomSpecs.FileHashes.Keys)
            {
                (await Extensions.WaitUntil(() => _dicomServer.Instances.ContainsKey(key), DicomScpWaitTimeSpan)).Should().BeTrue("{0} should be received", key);
            }

            // Send data received back to MIG
            var storeScu = _objectContainer.Resolve<IDataClient>("StoreSCU");

            var host = _configuration.InformaticsGatewayOptions.Host;
            var port = _informaticsGatewayConfiguration.Dicom.Scp.ExternalAppPort;

            _dataProvider.Workflows = null;
            _dataProvider.DicomSpecs.Files.Clear();
            _dataProvider.DicomSpecs.Files = new Dictionary<string, DicomFile>(_dicomServer.DicomFiles);
            _dataProvider.DicomSpecs.Files.Should().NotBeNull();

            await storeScu.SendAsync(
                _dataProvider,
                SourceAeTitle,
                host,
                port,
                MonaiAeTitle);
        }
    }
}
