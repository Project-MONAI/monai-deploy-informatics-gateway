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
using Ardalis.GuardClauses;
using BoDi;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Test.Plugins;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomWebStowServiceStepDefinitions
    {
        internal static readonly string[] DummyWorkflows = new string[] { "WorkflowA", "WorkflowB" };
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly Configurations _configurations;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly DataProvider _dataProvider;
        private readonly IDataClient _dataSink;
        private readonly Assertions _assertions;

        public DicomWebStowServiceStepDefinitions(ObjectContainer objectContainer, Configurations configuration)
        {
            _configurations = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _dataSink = objectContainer.Resolve<IDataClient>("DicomWebClient");
            _informaticsGatewayClient = objectContainer.Resolve<InformaticsGatewayClient>("InformaticsGatewayClient");
            _assertions = objectContainer.Resolve<Assertions>("Assertions");
        }

        [Given(@"a VirtualAE '(.*)'")]
        public async Task GivenAVirtualAE(string virtualAe)
        {
            Guard.Against.NullOrWhiteSpace(virtualAe, nameof(virtualAe));

            try
            {
                await _informaticsGatewayClient.VirtualAeTitle.Create(new VirtualApplicationEntity
                {
                    Name = virtualAe,
                    VirtualAeTitle = virtualAe,
                    Workflows = new List<string>(DummyWorkflows),
                    PluginAssemblies = new List<string>() { typeof(Monai.Deploy.InformaticsGateway.Test.Plugins.TestInputDataPluginVirtualAE).AssemblyQualifiedName }
                }, CancellationToken.None);

                _dataProvider.Workflows = DummyWorkflows;
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    await _informaticsGatewayClient.VirtualAeTitle.GetAeTitle(virtualAe, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
        }

        [Given(@"(.*) (.*) studies with '(.*)' grouping")]
        public void GivenNStudies(int studyCount, string modality, string grouping)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));

            _dataProvider.GenerateDicomData(modality, studyCount);
            _dataProvider.StudyGrouping = grouping;
            _receivedMessages.ClearMessages();
        }

        [Given(@"a workflow named '(.*)'")]
        public void GivenNStudies(string workflowName)
        {
            Guard.Against.NullOrWhiteSpace(workflowName, nameof(workflowName));

            _dataProvider.Workflows = new string[] { workflowName };
        }

        [When(@"the studies are uploaded to the DICOMWeb STOW-RS service at '([^']*)'")]
        public async Task WhenStudiesAreUploadedToTheDicomWebStowRSServiceWithoutStudyInstanceUID(string endpoint)
        {
            Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));

            await _dataSink.SendAsync(_dataProvider, $"{_configurations.InformaticsGatewayOptions.ApiEndpoint}{endpoint}", _dataProvider.Workflows, async (DicomWebClient dicomWebClient, DicomDataSpecs specs) =>
            {
                return await dicomWebClient.Stow.Store(specs.Files);
            });
            _dataProvider.ReplaceGeneratedDicomDataWithHashes();
        }

        [When(@"the studies are uploaded to the DICOMWeb STOW-RS service at '([^']*)' without overriding workflows")]
        public async Task WhenStudiesAreUploadedToTheDicomWebStowRSServiceWithoutOverridingWorkflows(string endpoint)
        {
            Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));

            await _dataSink.SendAsync(_dataProvider, $"{_configurations.InformaticsGatewayOptions.ApiEndpoint}{endpoint}", null, async (DicomWebClient dicomWebClient, DicomDataSpecs specs) =>
            {
                return await dicomWebClient.Stow.Store(specs.Files);
            });
            _dataProvider.ReplaceGeneratedDicomDataWithHashes();
        }

        [When(@"the studies are uploaded to the DICOMWeb STOW-RS service at '([^']*)' with StudyInstanceUid")]
        public async Task WhenStudiesAreUploadedToTheDicomWebStowRSServiceWithStudyInstanceUID(string endpoint)
        {
            Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));

            await _dataSink.SendAsync(_dataProvider, $"{_configurations.InformaticsGatewayOptions.ApiEndpoint}{endpoint}", _dataProvider.Workflows, async (DicomWebClient dicomWebClient, DicomDataSpecs specs) =>
            {
                // Note: the MIG DICOMweb client ignores instances without matching StudyInstanceUID.
                return await dicomWebClient.Stow.Store(specs.StudyInstanceUids.First(), specs.Files);
            });
            _dataProvider.ReplaceGeneratedDicomDataWithHashes();
        }

        [Then(@"studies are uploaded to storage service with data input VAE plugin")]
        public async Task ThenXXFilesUploadedToStorageServiceWithDataInputPlugins()
        {
            await _assertions.ShouldHaveUploadedDicomDataToMinio(
                _receivedMessages.Messages,
                _dataProvider.DicomSpecs.FileHashes,
                (dicomFile) =>
                {
                    dicomFile.Dataset.GetString(TestInputDataPluginVirtualAE.ExpectedTag)
                        .Should().Be(TestInputDataPluginVirtualAE.ExpectedValue);
                });
        }
    }
}
