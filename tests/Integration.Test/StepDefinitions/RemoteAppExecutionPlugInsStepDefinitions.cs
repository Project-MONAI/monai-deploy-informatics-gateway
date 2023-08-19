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
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Messaging.RabbitMQ;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class RemoteAppExecutionPlugInsStepDefinitions
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
        private readonly RabbitMQMessagePublisherService _messagePublisher;
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private Dictionary<string, DicomFile> _originalDicomFiles;
        private ExportRequestEvent _exportRequestEvent;
        private readonly Assertions _assertions;

        public RemoteAppExecutionPlugInsStepDefinitions(
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
            _messagePublisher = objectContainer.Resolve<RabbitMQMessagePublisherService>("MessagingPublisher");
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");

            DefaultDicomTags.AddRange(DicomTags);
            _dicomServer.ClearFilesAndUseHashes = false; //we need to store actual files to send the data back to MIG
        }

        [Given(@"a study that is exported to the test host")]
        public async Task AStudyThatIsExportedToTheTestHost()
        {
            // Register a new DICOM destination
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
            _exportRequestEvent = new ExportRequestEvent
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Destinations = new[] { _dicomDestination },
                ExportTaskId = Guid.NewGuid().ToString(),
                Files = _dataProvider.DicomSpecs.Files.Keys.ToList(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            };

            _exportRequestEvent.PluginAssemblies.Add(typeof(ExternalAppOutgoing).AssemblyQualifiedName);

            var message = new JsonMessage<ExportRequestEvent>(
                _exportRequestEvent,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                _exportRequestEvent.CorrelationId,
                string.Empty);

            _receivedExportCompletedMessages.ClearMessages();
            await _messagePublisher.Publish("md.export.request.monaiscu", message.ToMessage());
        }

        [When(@"the study is received and sent back to Informatics Gateway")]
        public async Task TheStudyIsReceivedAndSentBackToInformaticsGateway()
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
                    PlugInAssemblies = new List<string>() { typeof(ExternalAppIncoming).AssemblyQualifiedName }
                }, CancellationToken.None);
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

            // Wait for export completed event
            (await _receivedExportCompletedMessages.WaitforAsync(1, DicomScpWaitTimeSpan)).Should().BeTrue();

            foreach (var key in _dataProvider.DicomSpecs.FileHashes.Keys)
            {
                (await Extensions.WaitUntil(() => _dicomServer.Instances.ContainsKey(key), DicomScpWaitTimeSpan)).Should().BeTrue("{0} should be received", key);
            }

            // Send data received back to MIG
            var storeScu = _objectContainer.Resolve<IDataClient>("StoreSCU");

            var host = _configuration.InformaticsGatewayOptions.Host;
            var port = _informaticsGatewayConfiguration.Dicom.Scp.Port;

            _dataProvider.DicomSpecs.Files.Clear();
            _dataProvider.DicomSpecs.Files = new Dictionary<string, DicomFile>(_dicomServer.DicomFiles);
            _dataProvider.DicomSpecs.Files.Should().NotBeNull();

            await storeScu.SendAsync(
                _dataProvider,
                SourceAeTitle,
                host,
                port,
                MonaiAeTitle);

            _dataProvider.DimseRsponse.Should().Be(DicomStatus.Success);

            // Wait for workflow request events
            (await _receivedWorkflowRequestMessages.WaitforAsync(1, MessageWaitTimeSpan)).Should().BeTrue();
            _assertions.ShouldHaveCorrectNumberOfWorkflowRequestMessages(_dataProvider, _receivedWorkflowRequestMessages.Messages, 1);
        }

        [Then(@"ensure the original study and the received study are the same")]
        public async Task EnsureTheOriginalStudyAndTheReceivedStudyAreTheSameAsync()
        {
            var workflowRequestEvent = _receivedWorkflowRequestMessages.Messages[0].ConvertTo<WorkflowRequestEvent>();
            _exportRequestEvent.CorrelationId.Should().Be(_receivedWorkflowRequestMessages.Messages[0].CorrelationId);
            _exportRequestEvent.CorrelationId.Should().Be(workflowRequestEvent.CorrelationId);
            _exportRequestEvent.WorkflowInstanceId.Should().Be(workflowRequestEvent.WorkflowInstanceId);
            _exportRequestEvent.ExportTaskId.Should().Be(workflowRequestEvent.TaskId);
            await _assertions.ShouldRestoreAllDicomMetaata(_receivedWorkflowRequestMessages.Messages, _originalDicomFiles, DefaultDicomTags.ToArray()).ConfigureAwait(false);
        }
    }
}
