/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class AcrApiStepDefinitions
    {
        internal static readonly int WorkflowStudyCount = 1;

        internal static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromSeconds(30);
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly ObjectContainer _objectContainer;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configurations;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly DataProvider _dataProvider;
        private readonly Assertions _assertions;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;

        public AcrApiStepDefinitions(ObjectContainer objectContainer, ISpecFlowOutputHelper outputHelper, Configurations configuration)
        {
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configurations = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");
            _informaticsGatewayClient = objectContainer.Resolve<InformaticsGatewayClient>("InformaticsGatewayClient");
        }

        [Given(@"a DICOM study on a remote DICOMweb service")]
        public async Task GivenADICOMStudySentToAETFromWithTimeoutOfSeconds()
        {
            var modality = "US";
            _dataProvider.GenerateDicomData(modality, WorkflowStudyCount);
            _receivedMessages.SetupMessageHandle(WorkflowStudyCount);

            var storeScu = _objectContainer.Resolve<IDataClient>("StoreSCU");
            await storeScu.SendAsync(_dataProvider, "TEST-RUNNER", _configurations.OrthancOptions.Host, _configurations.OrthancOptions.DimsePort, "ORTHANC", TimeSpan.FromSeconds(300));
            _dataProvider.DimseRsponse.Should().Be(DicomStatus.Success);
        }

        [Given(@"an ACR API request to query & retrieve by (.*)")]
        public void GivenAnACRAPIRequestToQueryRetrieveByStudy(string requestType)
        {
            _dataProvider.GenerateAcrRequest(requestType);
            _dataProvider.ReplaceGeneratedDicomDataWithHashes();
        }

        [When(@"the ACR API request is sent")]
        public async Task WhenTheACRAPIRequestIsSentTo()
        {
            _outputHelper.WriteLine($"Sending inference request...");
            await _informaticsGatewayClient.Inference.NewInferenceRequest(_dataProvider.AcrRequest, CancellationToken.None);
        }

        [Then(@"a workflow requests sent to the message broker")]
        public void ThenAWorkflowRequestsSentToTheMessageBroker()
        {
            _receivedMessages.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            _assertions.ShouldHaveCorrectNumberOfWorkflowRequestMessagesAndAcrRequest(_dataProvider, _receivedMessages.Messages, WorkflowStudyCount);
        }

        [Then(@"a study is uploaded to the storage service")]
        public async Task ThenAStudyIsUploadedToTheStorageService()
        {
            _receivedMessages.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            _receivedMessages.Messages.Should().NotBeNullOrEmpty();
            await _assertions.ShouldHaveUploadedDicomDataToMinio(_receivedMessages.Messages, _dataProvider.DicomSpecs.FileHashes);
        }
    }
}
