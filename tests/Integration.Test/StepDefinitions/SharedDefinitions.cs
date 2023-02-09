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
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class SharedDefinitions
    {
        internal static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(10);
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly Assertions _assertions;
        private readonly DataProvider _dataProvider;

        public SharedDefinitions(ObjectContainer objectContainer)
        {
            if (objectContainer is null)
            {
                throw new ArgumentNullException(nameof(objectContainer));
            }

            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
        }

        [Given(@"(.*) (.*) studies")]
        public void GivenNStudies(int studyCount, string modality)
        {
            Guard.Against.NegativeOrZero(studyCount);
            Guard.Against.NullOrWhiteSpace(modality);

            _dataProvider.GenerateDicomData(modality, studyCount);

            _receivedMessages.ClearMessages();
        }

        [Then(@"(.*) workflow requests sent to message broker")]
        public async Task ThenWorkflowRequestSentToMessageBrokerAsync(int workflowCount)
        {
            Guard.Against.NegativeOrZero(workflowCount);

            (await _receivedMessages.WaitforAsync(workflowCount, MessageWaitTimeSpan)).Should().BeTrue();
            _assertions.ShouldHaveCorrectNumberOfWorkflowRequestMessages(_dataProvider, _receivedMessages.Messages, workflowCount);
        }

        [Then(@"studies are uploaded to storage service")]
        public async Task ThenXXFilesUploadedToStorageService()
        {
            await _assertions.ShouldHaveUploadedDicomDataToMinio(_receivedMessages.Messages, _dataProvider.DicomSpecs.FileHashes);
        }
    }
}
