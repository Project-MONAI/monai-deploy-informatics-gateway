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

using Ardalis.GuardClauses;
using BoDi;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class HealthLevel7Definitions
    {
        internal static readonly TimeSpan WaitTimeSpan = TimeSpan.FromMinutes(3);
        private readonly DataProvider _dataProvider;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly IDataClient _dataSink;
        private readonly Assertions _assertions;

        public HealthLevel7Definitions(ObjectContainer objectContainer)
        {
            if (objectContainer is null)
            {
                throw new ArgumentNullException(nameof(objectContainer));
            }

            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _dataSink = objectContainer.Resolve<IDataClient>("HL7Client");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");
        }

        [Given(@"HL7 messages in version (.*)")]
        public async Task GivenHl7MessagesInVersionX(string version)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));
            await _dataProvider.GenerateHl7Messages(version);
            _receivedMessages.SetupMessageHandle(1);
        }

        [When(@"the message are sent to Informatics Gateway")]
        public async Task WhenTheMessagesAreSentToInformaticsGateway()
        {
            await _dataSink.SendAsync(_dataProvider, false);
        }

        [When(@"the message are sent to Informatics Gateway in one batch")]
        public async Task WhenTheMessagesAreSentToInformaticsGatewayInOneBatch()
        {
            await _dataSink.SendAsync(_dataProvider, true);
        }

        [Then(@"acknowledgment are received")]
        public void ThenAcknowledgementAreReceived()
        {
            _assertions.ShoulddHaveCorrectNumberOfAckMessages(_dataProvider.HL7Specs.Responses);
        }

        [Then(@"a workflow requests sent to message broker")]
        public void ThenAWorkflowRequestIsSentToMessageBroker()
        {
            _receivedMessages.MessageWaitHandle.Wait(WaitTimeSpan).Should().BeTrue();
        }

        [Then(@"messages are uploaded to storage service")]
        public async Task ThenMessageAreUploadedToStorageService()
        {
            _assertions.ShouldHaveCorrectNumberOfWorkflowRequestMessagesAndHl7Messages(_dataProvider.HL7Specs, _receivedMessages.Messages, 1);
            await _assertions.ShouldHaveUploadedHl7ataToMinio(_receivedMessages.Messages);
        }
    }
}
