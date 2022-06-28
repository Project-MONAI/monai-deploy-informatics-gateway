// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Net;
using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomDimseScpServicesStepDefinitions
    {
        internal static readonly string[] DummyWorkflows = new string[] { "WorkflowA", "WorkflowB" };
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly RabbitMqHooks _rabbitMqHooks;
        private readonly DicomScu _dicomScu;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;

        public DicomDimseScpServicesStepDefinitions(
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            DicomInstanceGenerator dicomInstanceGenerator,
            DicomScu dicomScu,
            InformaticsGatewayClient informaticsGatewayClient,
            RabbitMqHooks rabbitMqHooks)
        {
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomInstanceGenerator = dicomInstanceGenerator ?? throw new ArgumentNullException(nameof(dicomInstanceGenerator));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));
            _dicomScu = dicomScu ?? throw new ArgumentNullException(nameof(dicomScu));
            _informaticsGatewayClient = informaticsGatewayClient ?? throw new ArgumentNullException(nameof(informaticsGatewayClient));
            _informaticsGatewayClient.ConfigureServiceUris(new Uri(_configuration.InformaticsGatewayOptions.ApiEndpoint));
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
                    HostIp = _configuration.TestRunnerOptions.HostIp,
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.BadRequest &&
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

            _outputHelper.WriteLine($"Generating {studyCount} {modality} study");
            _configuration.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configuration.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, seriesPerStudy, modality, studySpec);
            _scenarioContext[SharedDefinitions.KeyDicomFiles] = fileSpecs;
            _rabbitMqHooks.SetupMessageHandle(fileSpecs.NumberOfExpectedRequests(_scenarioContext[SharedDefinitions.KeyDataGrouping].ToString()));
            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount}, {fileSpecs.SeriesPerStudyCount}, {fileSpecs.InstancePerSeries}, {fileSpecs.FileCount}");
        }

        [Given(@"a called AE Title named '([^']*)' that groups by '([^']*)' for (.*) seconds")]
        public async Task GivenACalledAETitleNamedThatGroupsByForSeconds(string calledAeTitle, string grouping, uint groupingTimeout)
        {
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(grouping, nameof(grouping));
            Guard.Against.NegativeOrZero(groupingTimeout, nameof(groupingTimeout));

            _scenarioContext[SharedDefinitions.KeyDataGrouping] = grouping;
            try
            {
                _scenarioContext[SharedDefinitions.KeyCalledAet] = await _informaticsGatewayClient.MonaiScpAeTitle.Create(new MonaiApplicationEntity
                {
                    AeTitle = calledAeTitle,
                    Name = calledAeTitle,
                    Grouping = grouping,
                    Timeout = groupingTimeout,
                    Workflows = new List<string>(DummyWorkflows)
                }, CancellationToken.None);
                _scenarioContext[SharedDefinitions.KeyWorkflows] = DummyWorkflows;
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.BadRequest &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    _scenarioContext[SharedDefinitions.KeyCalledAet] = await _informaticsGatewayClient.MonaiScpAeTitle.GetAeTitle(calledAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
        }

        [When(@"a C-ECHO-RQ is sent to '([^']*)' from '([^']*)' with timeout of (.*) seconds")]
        public async Task WhenAC_ECHO_RQIsSentToFromWithTimeoutOfSeconds(string calledAeTitle, string callingAeTitle, int clientTimeoutSeconds)
        {
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(callingAeTitle, nameof(callingAeTitle));
            Guard.Against.NegativeOrZero(clientTimeoutSeconds, nameof(clientTimeoutSeconds));

            _scenarioContext[SharedDefinitions.KeyDimseResponse] = await _dicomScu.CEcho(
                _configuration.InformaticsGatewayOptions.Host,
                _configuration.InformaticsGatewayOptions.DimsePort,
                callingAeTitle,
                calledAeTitle,
                TimeSpan.FromSeconds(clientTimeoutSeconds));
        }

        [Then(@"a successful response should be received")]
        public void ThenASuccessfulResponseShouldBeReceived()
        {
            (_scenarioContext[SharedDefinitions.KeyDimseResponse] as DicomStatus).Should().Be(DicomStatus.Success);
        }

        [When(@"a C-STORE-RQ is sent to '([^']*)' with AET '([^']*)' from '([^']*)' with timeout of (.*) seconds")]
        public async Task WhenAC_STORE_RQIsSentToWithAETFromWithTimeoutOfSeconds(string application, string calledAeTitle, string callingAeTitle, int clientTimeoutSeconds)
        {
            Guard.Against.NullOrWhiteSpace(application, nameof(application));
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(callingAeTitle, nameof(callingAeTitle));
            Guard.Against.NegativeOrZero(clientTimeoutSeconds, nameof(clientTimeoutSeconds));

            var host = _configuration.InformaticsGatewayOptions.Host;
            var port = _configuration.InformaticsGatewayOptions.DimsePort;

            var dicomFileSpec = _scenarioContext[SharedDefinitions.KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;
            dicomFileSpec.Should().NotBeNull();
            _scenarioContext[SharedDefinitions.KeyDimseResponse] = await _dicomScu.CStore(
                host,
                port,
                callingAeTitle,
                calledAeTitle,
                dicomFileSpec.Files,
                TimeSpan.FromSeconds(clientTimeoutSeconds));

            // Remove after sent to reduce memory usage
            var dicomFileSize = new Dictionary<string, string>();
            foreach (var dicomFile in dicomFileSpec.Files)
            {
                var key = dicomFile.GenerateFileName();
                dicomFileSize[key] = dicomFile.CalculateHash();
            }

            _scenarioContext[SharedDefinitions.KeyDicomHashes] = dicomFileSize;
            dicomFileSpec.Files.Clear();
        }
    }
}
