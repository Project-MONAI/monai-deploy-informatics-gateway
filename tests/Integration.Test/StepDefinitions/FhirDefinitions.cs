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

using Ardalis.GuardClauses;
using BoDi;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class FhirDefinitions
    {
        internal enum FileFormat
        { Xml, Json };

        internal static readonly TimeSpan WaitTimeSpan = TimeSpan.FromSeconds(120);
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly RabbitMqConsumer _artifactReceivedMessages;
        private readonly DataProvider _dataProvider;
        private readonly Assertions _assertions;
        private readonly IDataClient _dataSink;

        public FhirDefinitions(ObjectContainer objectContainer)
        {
            if (objectContainer is null)
            {
                throw new ArgumentNullException(nameof(objectContainer));
            }

            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _artifactReceivedMessages = objectContainer.Resolve<RabbitMqConsumer>("ArtifactRecievedSubscriber");
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");
            _dataSink = objectContainer.Resolve<IDataClient>("FhirClient");

            _dataProvider.Source = "::ffff:127.0.0.1";
        }

        [Given(@"FHIR message (.*) in (.*)")]
        public async Task GivenHl7MessagesInVersionX(string version, string format)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));
            Guard.Against.NullOrWhiteSpace(format, nameof(format));

            await _dataProvider.GenerateFhirMessages(version, format);
            _receivedMessages.ClearMessages();
        }

        [When(@"the FHIR messages are sent to Informatics Gateway")]
        public async Task WhenTheMessagesAreSentToInformaticsGateway()
        {
            await _dataSink.SendAsync(_dataProvider);
        }

        [Then(@"workflow requests are sent to message broker")]
        public async Task ThenWorkflowRequestAreSentToMessageBrokerAsync()
        {
            (await _receivedMessages.WaitforAsync(_dataProvider.FhirSpecs.Files.Count, WaitTimeSpan)).Should().BeTrue();
            _assertions.ShouldHaveCorrectNumberOfWorkflowRequestMessagesForFhirRequest(_dataProvider, Messaging.Events.DataService.FHIR, _receivedMessages.Messages, _dataProvider.FhirSpecs.Files.Count);
        }

        [Then(@"FHIR resources are uploaded to storage service")]
        public async Task ThenFhirResourcesAreUploadedToStorageService()
        {
            _receivedMessages.Messages.Should().NotBeNullOrEmpty().And.HaveCount(_dataProvider.FhirSpecs.Files.Count);
            await _assertions.ShouldHaveUploadedFhirDataToMinio(_receivedMessages.Messages, _dataProvider.FhirSpecs.Files);
        }
    }
}
