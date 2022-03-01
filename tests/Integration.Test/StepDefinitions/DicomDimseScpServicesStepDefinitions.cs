// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Net;
using System.Text;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Serialization;
using FluentAssertions.Execution;
using Minio;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using Polly;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class DicomDimseScpServicesStepDefinitions
    {
        internal static readonly string[] DummyWorkflows = new string[] { "WorkflowA", "WorkflowB" };
        internal static readonly string KeyDicomHashes = "DICOM_HASHES";
        internal static readonly string KeyDicomFiles = "DICOM_FILES";
        internal static readonly string KeyCalledAet = "CALLED_AET";
        internal static readonly string KeyDataGrouping = "DICOM_DATA_GROUPING";
        internal static readonly string KeyDimseResponse = "DIMSE_RESPONSE";

        internal static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(3);

        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly DicomScu _dicomScu;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly RabbitMqHooks _rabbitMqHooks;

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
            _dicomScu = dicomScu ?? throw new ArgumentNullException(nameof(dicomScu));
            _informaticsGatewayClient = informaticsGatewayClient ?? throw new ArgumentNullException(nameof(informaticsGatewayClient));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));
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
                    await _informaticsGatewayClient.DicomSources.Get(callingAeTitle, CancellationToken.None);
                }
                else
                {
                    throw;
                }
            }
        }

        [Given(@"(.*) (.*) studies")]
        public void GivenNStudies(int studyCount, string modality)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));

            _outputHelper.WriteLine($"Generating {studyCount} {modality} study");
            _configuration.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configuration.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, modality, studySpec);
            _scenarioContext[KeyDicomFiles] = fileSpecs;
            _rabbitMqHooks.SetupMessageHandle(fileSpecs.NumberOfExpectedRequests(_scenarioContext[KeyDataGrouping].ToString()));
            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount}, {fileSpecs.SeriesPerStudyCount}, {fileSpecs.InstancePerSeries}, {fileSpecs.FileCount}");
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
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, seriesPerStudy, modality, studySpec);
            _scenarioContext[KeyDicomFiles] = fileSpecs;
            _rabbitMqHooks.SetupMessageHandle(fileSpecs.NumberOfExpectedRequests(_scenarioContext[KeyDataGrouping].ToString()));
            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount}, {fileSpecs.SeriesPerStudyCount}, {fileSpecs.InstancePerSeries}, {fileSpecs.FileCount}");
        }

        [Given(@"a called AE Title named '([^']*)' that groups by '([^']*)' for (.*) seconds")]
        public async Task GivenACalledAETitleNamedThatGroupsByForSeconds(string calledAeTitle, string grouping, uint groupingTimeout)
        {
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(grouping, nameof(grouping));
            Guard.Against.NegativeOrZero(groupingTimeout, nameof(groupingTimeout));

            _scenarioContext[KeyDataGrouping] = grouping;
            try
            {
                _scenarioContext[KeyCalledAet] = await _informaticsGatewayClient.MonaiScpAeTitle.Create(new MonaiApplicationEntity
                {
                    AeTitle = calledAeTitle,
                    Name = calledAeTitle,
                    Grouping = grouping,
                    Timeout = groupingTimeout,
                    Workflows = new List<string>(DummyWorkflows)
                }, CancellationToken.None);
            }
            catch (ProblemException ex)
            {
                if (ex.ProblemDetails.Status == (int)HttpStatusCode.BadRequest &&
                    ex.ProblemDetails.Detail.Contains("already exists"))
                {
                    _scenarioContext[KeyCalledAet] = await _informaticsGatewayClient.MonaiScpAeTitle.Get(calledAeTitle, CancellationToken.None);
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

            _scenarioContext[KeyDimseResponse] = await _dicomScu.CEcho(
                _configuration.InformaticsGatewayOptions.Host,
                _configuration.InformaticsGatewayOptions.DimsePort,
                callingAeTitle,
                calledAeTitle,
                TimeSpan.FromSeconds(clientTimeoutSeconds));
        }

        [Then(@"a successful response should be received")]
        public void ThenASuccessfulResponseShouldBeReceived()
        {
            (_scenarioContext[KeyDimseResponse] as DicomStatus).Should().Be(DicomStatus.Success);
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

            var dicomFileSpec = _scenarioContext[KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;
            dicomFileSpec.Should().NotBeNull();
            _scenarioContext[KeyDimseResponse] = await _dicomScu.CStore(
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
                string key = dicomFile.GenerateFileName();
                dicomFileSize[key] = dicomFile.CalculateHash();
            }

            _scenarioContext[KeyDicomHashes] = dicomFileSize;
            dicomFileSpec.Files.Clear();
        }

        [Then(@"(.*) studies are uploaded to storage service")]
        public async Task ThenXXFilesUploadedToStorageService(int studyCount)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));

            var minioClient = new MinioClient(_configuration.StorageServiceOptions.Endpoint, _configuration.StorageServiceOptions.AccessKey, _configuration.StorageServiceOptions.AccessToken);

            var dicomSizes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;
            _rabbitMqHooks.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            messages.Should().NotBeNullOrEmpty();

            foreach (var message in messages)
            {
                var request = message.ConvertTo<WorkflowRequestMessage>();
                foreach (BlockStorageInfo file in request.Payload)
                {
                    var dicomValidationKey = string.Empty;
                    await minioClient.GetObjectAsync(file.Bucket, $"{request.PayloadId}/{file.Path}", (stream) =>
                    {
                        using var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        var dicomFile = DicomFile.Open(memoryStream);
                        dicomValidationKey = dicomFile.GenerateFileName();
                        dicomSizes.Should().ContainKey(dicomValidationKey).WhoseValue.Should().Be(dicomFile.CalculateHash());
                    });

                    await minioClient.GetObjectAsync(file.Bucket, $"{request.PayloadId}/{file.Metadata}", (stream) =>
                    {
                        using var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        var json = Encoding.UTF8.GetString(memoryStream.ToArray());

                        var dicomFileFromJson = DicomJson.ConvertJsonToDicom(json);
                        var key = dicomFileFromJson.GenerateFileName();
                        key.Should().Be(dicomValidationKey);
                    });
                }
            }
        }

        [Then(@"(.*) workflow requests sent to message broker")]
        public void ThenWorkflowRequestSentToMessageBroker(int workflowCount)
        {
            Guard.Against.NegativeOrZero(workflowCount, nameof(workflowCount));

            _rabbitMqHooks.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            var fileSpecs = _scenarioContext[KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;

            messages.Should().NotBeNullOrEmpty().And.HaveCount(fileSpecs.NumberOfExpectedRequests(_scenarioContext[KeyDataGrouping].ToString()));
            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBase.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestMessage>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be((fileSpecs.NumberOfExpectedFiles(_scenarioContext[KeyDataGrouping].ToString())));
                request.Workflows.Should().Equal(DummyWorkflows);
            }
        }

        [Then(@"the temporary data directory has been cleared")]
        public void ThenTheTemporaryDataDirectoryHasBeenCleared()
        {
            Policy
                .Handle<AssertionFailedException>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromMilliseconds(150 * retryAttempt), (exception, retryCount, context) =>
                {
                    _outputHelper.WriteLine("Exception 'validating temporary data directory': {0}", exception.Message);
                })
                .Execute(() =>
                {
                    var calledAet = _scenarioContext[KeyCalledAet] as MonaiApplicationEntity;
                    var aeTitleDir = Path.Combine(_configuration.InformaticsGatewayOptions.TemporaryDataStore, calledAet.AeTitle);
                    _outputHelper.WriteLine($"Validating AE Title data dir {aeTitleDir}");

                    if (Directory.Exists(aeTitleDir))
                    {
                        var files = Directory.GetFiles(aeTitleDir, "*", SearchOption.AllDirectories);
                        files.Length.Should().Be(0);
                    }
                });
        }
    }
}
