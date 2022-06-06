// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Net;
using Ardalis.GuardClauses;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomWebStowServiceStepDefinitions
    {
        protected readonly ScenarioContext _scenarioContext;
        protected readonly ISpecFlowOutputHelper _outputHelper;
        protected readonly Configurations _configuration;
        protected readonly DicomInstanceGenerator _dicomInstanceGenerator;
        protected readonly RabbitMqHooks _rabbitMqHooks;
        public DicomWebStowServiceStepDefinitions(
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            DicomInstanceGenerator dicomInstanceGenerator,
            RabbitMqHooks rabbitMqHooks)
        {
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomInstanceGenerator = dicomInstanceGenerator ?? throw new ArgumentNullException(nameof(dicomInstanceGenerator));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));
        }

        [Given(@"a workflow named '(.*)'")]
        public void GivenNStudies(string workflowName)
        {
            Guard.Against.NullOrWhiteSpace(workflowName, nameof(workflowName));

            _scenarioContext[SharedDefinitions.KeyWorkflows] = new string[] { workflowName };
        }

        [When(@"the studies are uploaded to the DICOMWeb STOW-RS service at '([^']*)'")]
        public async Task WhenStudiesAreUploadedToTheDicomWebStowRSServiceWithoutStudyInstanceUID(string endpoint)
        {
            Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));

            await UploadStudies(endpoint, async (DicomWebClient dicomWebClient, DicomInstanceGenerator.StudyGenerationSpecs specs) =>
            {
                return await dicomWebClient.Stow.Store(specs.Files);
            });
        }

        [When(@"the studies are uploaded to the DICOMWeb STOW-RS service at '([^']*)' with StudyInstanceUid")]
        public async Task WhenStudiesAreUploadedToTheDicomWebStowRSServiceWithStudyInstanceUID(string endpoint)
        {
            Guard.Against.NullOrWhiteSpace(endpoint, nameof(endpoint));

            await UploadStudies(endpoint, async (DicomWebClient dicomWebClient, DicomInstanceGenerator.StudyGenerationSpecs specs) =>
            {
                // Note: the MIG DICOMweb client ignores instances without matching StudyInstanceUID.
                return await dicomWebClient.Stow.Store(specs.StudyInstanceUids.First(), specs.Files);
            });
        }

        private async Task UploadStudies(string endpoint, Func<DicomWebClient, DicomInstanceGenerator.StudyGenerationSpecs, Task<DicomWebResponse<string>>> stowFunc)
        {
            var dicomFileSpec = _scenarioContext[SharedDefinitions.KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;
            dicomFileSpec.Should().NotBeNull();
            dicomFileSpec.StudyInstanceUids.Should().NotBeNullOrEmpty();

            _outputHelper.WriteLine($"POSTing studies to {endpoint} with {dicomFileSpec.Files.Count} files...");
            var httpClient = new HttpClient();
            var dicomWebClient = new DicomWebClient(httpClient, null);
            dicomWebClient.ConfigureServiceUris(new Uri($"{_configuration.InformaticsGatewayOptions.ApiEndpoint}{endpoint}"));

            if (_scenarioContext.ContainsKey(SharedDefinitions.KeyWorkflows))
            {
                var workflows = _scenarioContext[SharedDefinitions.KeyWorkflows] as string[];
                workflows.Should().NotBeNullOrEmpty();
                dicomWebClient.ConfigureServicePrefix(DicomWebServiceType.Stow, $"{workflows.First()}/");
                _outputHelper.WriteLine($"configured STOW service prefix = {workflows.First()}...");
            }

            var results = await stowFunc(dicomWebClient, dicomFileSpec);
            results.StatusCode.Should().Be(HttpStatusCode.OK);

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
