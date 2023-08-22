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
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomDimseScpServicesStepDefinitions
    {
        internal static readonly string[] DummyWorkflows = new string[] { "WorkflowA", "WorkflowB" };
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly ObjectContainer _objectContainer;
        private readonly Configurations _configuration;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly RabbitMqConsumer _receivedMessages;
        private readonly DataProvider _dataProvider;

        public DicomDimseScpServicesStepDefinitions(
            ObjectContainer objectContainer,
            Configurations configuration)
        {
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
            _dataProvider = objectContainer.Resolve<DataProvider>("DataProvider");
            _informaticsGatewayClient = objectContainer.Resolve<InformaticsGatewayClient>("InformaticsGatewayClient");
        }

        [Given(@"a calling AE Title '([^']*)'")]
        public async Task GivenACallingAETitle(string callingAeTitle)
        {
            Guard.Against.NullOrWhiteSpace(callingAeTitle, nameof(callingAeTitle));

            try
            {
                await _informaticsGatewayClient.DicomSources.Create(new SourceApplicationEntity
                {
                    Name = callingAeTitle,
                    AeTitle = callingAeTitle,
                    HostIp = _configuration.InformaticsGatewayOptions.Host,
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    await _informaticsGatewayClient.DicomSources.GetAeTitle(callingAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
        }

        [Given(@"(.*) (.*) studies with (.*) series per study")]
        public void GivenXStudiesWithYSeriesPerStudy(int studyCount, string modality, int seriesPerStudy)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));
            Guard.Against.NegativeOrZero(seriesPerStudy, nameof(seriesPerStudy));

            _dataProvider.GenerateDicomData(modality, studyCount, seriesPerStudy);

            _receivedMessages.ClearMessages();
        }

        [Given(@"a called AE Title named '([^']*)' that groups by '([^']*)' for (.*) seconds")]
        public async Task GivenACalledAETitleNamedThatGroupsByForSeconds(string calledAeTitle, string grouping, uint groupingTimeout)
        {
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(grouping, nameof(grouping));
            Guard.Against.NegativeOrZero(groupingTimeout, nameof(groupingTimeout));

            _dataProvider.StudyGrouping = grouping;
            try
            {
                await _informaticsGatewayClient.MonaiScpAeTitle.Create(new MonaiApplicationEntity
                {
                    AeTitle = calledAeTitle,
                    Name = calledAeTitle,
                    Grouping = grouping,
                    Timeout = groupingTimeout,
                    Workflows = new List<string>(DummyWorkflows),
                    PlugInAssemblies = new List<string>() { typeof(Monai.Deploy.InformaticsGateway.Test.PlugIns.TestInputDataPlugInModifyDicomFile).AssemblyQualifiedName }
                }, CancellationToken.None);

                _dataProvider.Workflows = DummyWorkflows;
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.Conflict &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    await _informaticsGatewayClient.MonaiScpAeTitle.GetAeTitle(calledAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
        }

        [Given(@"a DICOM client configured with (.*) seconds timeout")]
        public void GivenADICOMClientConfiguredWithSecondsTimeout(int timeout)
        {
            Guard.Against.NegativeOrZero(timeout, nameof(timeout));
            _dataProvider.ClientTimeout = timeout;
        }

        [Given(@"a DICOM client configured to send data over (.*) associations and wait (.*) between each association")]
        public void GivenADICOMClientConfiguredToSendDataOverAssociationsAndWaitSecondsBetweenEachAssociation(int associations, int pulseTime)
        {
            Guard.Against.NegativeOrZero(associations, nameof(associations));
            Guard.Against.Negative(pulseTime, nameof(associations));

            _dataProvider.ClientSendOverAssociations = associations;
            _dataProvider.ClientAssociationPulseTime = pulseTime;
        }

        [When(@"a C-ECHO-RQ is sent to '([^']*)' from '([^']*)'")]
        public async Task WhenAC_ECHO_RQIsSentToFromWithTimeoutOfSeconds(string calledAeTitle, string callingAeTitle)
        {
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(callingAeTitle, nameof(callingAeTitle));

            var echoScu = _objectContainer.Resolve<IDataClient>("EchoSCU");
            await echoScu.SendAsync(
                _dataProvider,
                callingAeTitle,
                _configuration.InformaticsGatewayOptions.Host,
                _informaticsGatewayConfiguration.Dicom.Scp.Port,
                calledAeTitle,
                TimeSpan.FromSeconds(_dataProvider.ClientTimeout));
        }

        [Then(@"a successful response should be received")]
        public void ThenASuccessfulResponseShouldBeReceived()
        {
            _dataProvider.DimseRsponse.Should().Be(DicomStatus.Success);
        }

        [When(@"a C-STORE-RQ is sent to '([^']*)' with AET '([^']*)' from '([^']*)'")]
        [When(@"C-STORE-RQ are sent to '([^']*)' with AET '([^']*)' from '([^']*)'")]
        public async Task WhenAC_STORE_RQIsSentToWithAETFromWithTimeoutOfSeconds(string application, string calledAeTitle, string callingAeTitle)
        {
            Guard.Against.NullOrWhiteSpace(application, nameof(application));
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(callingAeTitle, nameof(callingAeTitle));

            var storeScu = _objectContainer.Resolve<IDataClient>("StoreSCU");

            var host = _configuration.InformaticsGatewayOptions.Host;
            var port = _informaticsGatewayConfiguration.Dicom.Scp.Port;

            var dicomFileSpec = _dataProvider.DicomSpecs;
            dicomFileSpec.Should().NotBeNull();
            await storeScu.SendAsync(
                _dataProvider,
                callingAeTitle,
                host,
                port,
                calledAeTitle);

            _dataProvider.ReplaceGeneratedDicomDataWithHashes();
        }
    }
}
