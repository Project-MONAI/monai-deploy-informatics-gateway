/*
 * Copyright 2023 MONAI Consortium
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

using BoDi;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Messaging.RabbitMQ;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]

    internal class Hl7StepDEfinitions
    {
        private static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(3);
        private static readonly DicomTag[] DicomTags = new[] { DicomTag.AccessionNumber, DicomTag.StudyDescription, DicomTag.SeriesDescription, DicomTag.PatientAddress, DicomTag.PatientAge, DicomTag.PatientName };
        private static readonly List<DicomTag> DefaultDicomTags = new() { DicomTag.PatientID, DicomTag.StudyInstanceUID, DicomTag.SeriesInstanceUID, DicomTag.SOPInstanceUID };

        private readonly ObjectContainer _objectContainer;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly IDataClient _dataSinkMinio;
        private readonly DicomScp _dicomServer;
        private readonly Configurations _configuration;
        private string _dicomDestination;
        private readonly DataProvider _dataProvider;
        private readonly RabbitMqConsumer _receivedExportHL7CompletedMessages;
        private readonly RabbitMQMessagePublisherService _messagePublisher;
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private Dictionary<string, HL7.Dotnetcore.Message> _originalHL7Files;
        private ExportRequestEvent _exportRequestEvent;
        private readonly Assertions _assertions;
        private readonly string _correlationId = Guid.NewGuid().ToString();
        private readonly string _exportTaskId = Guid.NewGuid().ToString();
        private readonly string _workflowInstanceId = Guid.NewGuid().ToString();
        internal static readonly TimeSpan WaitTimeSpan = TimeSpan.FromSeconds(30);
        private readonly string _hl7SendAddress = "127.0.0.1";
        private readonly int _hl7Port = 2574;
        private JsonMessage<ExportRequestEvent> _messageToSend;

        private readonly List<HL7.Dotnetcore.Message> _hl7Messages = new List<HL7.Dotnetcore.Message>();
        private TcpListener _tcpListener;

        public Hl7StepDEfinitions(ObjectContainer objectContainer, Configurations configuration)
        {
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _informaticsGatewayClient = objectContainer.Resolve<InformaticsGatewayClient>("InformaticsGatewayClient");
            _dataSinkMinio = objectContainer.Resolve<IDataClient>("MinioClient");
            _dicomServer = objectContainer.Resolve<DicomScp>("DicomScp");
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _receivedExportHL7CompletedMessages = objectContainer.Resolve<RabbitMqConsumer>("ExportHL7CompleteSubscriber");
            _messagePublisher = objectContainer.Resolve<RabbitMQMessagePublisherService>("MessagingPublisher");
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");

            DefaultDicomTags.AddRange(DicomTags);
            _dicomServer.ClearFilesAndUseHashes = false; //we need to store actual files to send the data back to MIG
        }

        [Given(@"a HL7 message that is exported to the test host")]
        public async Task GivenAHLMessageThatIsExportedToTheTestHost()
        {
            HL7DestinationEntity destination;
            try
            {
                destination = await _informaticsGatewayClient.HL7Destinations.Create(new HL7DestinationEntity
                {
                    Name = _dicomServer.FeatureScpAeTitle,
                    HostIp = _hl7SendAddress,
                    Port = _hl7Port
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict && ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    destination = await _informaticsGatewayClient.HL7Destinations.GetAeTitle(_dicomServer.FeatureScpAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
            _dicomDestination = destination.Name;

            // Generate a study with multiple series
            //_dataProvider.GenerateDicomData("MG", 1, 1);
            await _dataProvider.GenerateHl7Messages("2.3");

            _originalHL7Files = new Dictionary<string, HL7.Dotnetcore.Message>(_dataProvider.HL7Specs.Files);

            var path = "hl7filepath";
            await _dataSinkMinio.SaveHl7Async(_dataProvider, path);

            // Emit a export request event
            _exportRequestEvent = new ExportRequestEvent
            {
                CorrelationId = _correlationId,
                Destinations = new string[] { destination.Name },
                ExportTaskId = _exportTaskId,
                Files = _dataProvider.HL7Specs.Files.Keys.Select(f => $"{path}/{f.Replace(".txt", ".hl7")}"),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = _workflowInstanceId,
                PayloadId = "ThisIs/My/Output/Folder",
            };

            _messageToSend = new JsonMessage<ExportRequestEvent>(
                _exportRequestEvent,
                MessageBrokerConfiguration.InformaticsGatewayApplicationId,
                _exportRequestEvent.CorrelationId,
                string.Empty);

        }

        [When(@"the HL7 Export message is received with (.*) messages acked (.*)")]
        public async Task WhenTheHL7ExportMessageIsReceivedWithMessagesAcked(int messageCount, bool acked)
        {
            var cancellationToken = new CancellationToken();

            _tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Parse(_hl7SendAddress), _hl7Port);
            _tcpListener.Start();

            await _messagePublisher.Publish("md.export.hl7", _messageToSend.ToMessage());

            List<HL7.Dotnetcore.Message> recievedMessages = new List<HL7.Dotnetcore.Message>();

            for (int i = 0; i < messageCount; i++)
            {
                using var _client = await _tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                await GetMessageAsync(_client, acked, cancellationToken);
                if (_hl7Messages.Count == messageCount)
                { break; }
            }

            _tcpListener.Stop();
        }

        private async Task GetMessageAsync(TcpClient _client, bool acked, CancellationToken cancellationToken)
        {
            var messages = new List<HL7.Dotnetcore.Message>();
            using var _clientStream = _client.GetStream();
            _clientStream.ReadTimeout = 5000;
            _clientStream.WriteTimeout = 5000;
            var buffer = new byte[10240];

            var s_cts = new CancellationTokenSource();
            s_cts.CancelAfter(60000);

            var bytesRead = _clientStream.Read(buffer, 0, buffer.Length);


            if (bytesRead == 0 || s_cts.IsCancellationRequested)
            {
                return;
            }

            var data = Encoding.UTF8.GetString(buffer.ToArray());

            var _rawHl7Messages = HL7.Dotnetcore.MessageHelper.ExtractMessages(data);
            foreach (var message in _rawHl7Messages)
            {
                var hl7Message = new HL7.Dotnetcore.Message(message);
                hl7Message.ParseMessage();
                _hl7Messages.Add(hl7Message);
                if (acked)
                { await SendAcknowledgment(_clientStream, hl7Message, cancellationToken); }
            }
            return;
        }

        [Then(@"ensure that exportcomplete messages are sent with (.*)")]
        public async Task ThenEnsureThatExportcompleteMessagesAreSentWithSuscess(string valid)
        {
            var success = await _receivedExportHL7CompletedMessages.WaitforAsync(1, TimeSpan.FromSeconds(600));
            Assert.Equal(1, _receivedExportHL7CompletedMessages.Messages.Count);
            var message = _receivedExportHL7CompletedMessages.Messages.First();
            var exportEvent = message.ConvertTo<ExportCompleteEvent>();
            var status = exportEvent.Status;
            if (valid == "success")
            {
                Assert.Equal(ExportStatus.Success, status);
            }
            else if (valid == "failure")
            {
                Assert.Equal(ExportStatus.Failure, status);
            }

            foreach (var hl7message in _hl7Messages)
            {
                Assertions.ShouldBeInMessageDictionary(_originalHL7Files, hl7message);
            }
        }

        private async Task SendAcknowledgment(NetworkStream networkStream, HL7.Dotnetcore.Message message, CancellationToken cancellationToken)
        {
            if (message == null) { return; }
            var ackMessage = message.GetACK(true);
            var ackData = new ReadOnlyMemory<byte>(ackMessage.GetMLLP());
            {
                try
                {
                    await networkStream.WriteAsync(ackData, cancellationToken).ConfigureAwait(false);
                    await networkStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
