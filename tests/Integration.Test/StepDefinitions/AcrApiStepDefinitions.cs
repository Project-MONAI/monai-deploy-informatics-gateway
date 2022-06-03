// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Serialization;
using FluentAssertions.Execution;
using Minio;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using Polly;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class AcrApiStepDefinitions
    {
        internal static readonly string KeyDicomFiles = "DICOM_FILES";
        internal static readonly string KeyInferenceRequest = "INFERENCE_REQUEST";
        internal static readonly string KeyDicomHashes = "DICOM_HASHES";
        internal static readonly int WorkflowStudyCount = 1;

        internal static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(1);

        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly DicomScu _dicomScu;
        private readonly InformaticsGatewayClient _informaticsGatewayClient;
        private readonly RabbitMqHooks _rabbitMqHooks;

        public AcrApiStepDefinitions(
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

        [Given(@"a DICOM study on a remote DICOMweb service")]
        public async Task GivenADICOMStudySentToAETFromWithTimeoutOfSeconds()
        {
            var modality = "US";
            _outputHelper.WriteLine($"Generating {WorkflowStudyCount} {modality} study");
            _configuration.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configuration.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, WorkflowStudyCount, modality, studySpec);
            _scenarioContext[KeyDicomFiles] = fileSpecs;
            _rabbitMqHooks.SetupMessageHandle(WorkflowStudyCount);
            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount}, {fileSpecs.SeriesPerStudyCount}, {fileSpecs.InstancePerSeries}, {fileSpecs.FileCount}");
            var result = await _dicomScu.CStore(_configuration.OrthancOptions.Host, _configuration.OrthancOptions.DimsePort, "TEST-RUNNER", "ORTHANC", fileSpecs.Files, TimeSpan.FromSeconds(300));
            result.Should().Be(DicomStatus.Success);

            // Remove after sent to reduce memory usage
            var dicomFileSize = new Dictionary<string, string>();
            foreach (var dicomFile in fileSpecs.Files)
            {
                var key = dicomFile.GenerateFileName();
                dicomFileSize[key] = dicomFile.CalculateHash();
            }

            _scenarioContext[KeyDicomHashes] = dicomFileSize;
        }

        [Given(@"an ACR API request to query & retrieve by (.*)")]
        public void GivenAnACRAPIRequestToQueryRetrieveByStudy(string requestType)
        {
            var fileSpecs = _scenarioContext[KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;

            var inferenceRequest = new InferenceRequest();
            inferenceRequest.TransactionId = Guid.NewGuid().ToString();
            inferenceRequest.InputMetadata = new InferenceRequestMetadata();
            inferenceRequest.InputMetadata.Details = new InferenceRequestDetails();
            switch (requestType)
            {
                case "Study":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.DicomUid;
                    inferenceRequest.InputMetadata.Details.Studies = new List<RequestedStudy>();
                    inferenceRequest.InputMetadata.Details.Studies.Add(new RequestedStudy
                    {
                        StudyInstanceUid = fileSpecs.StudyInstanceUids[0],
                    });
                    break;

                case "Patient":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.DicomPatientId;
                    inferenceRequest.InputMetadata.Details.PatientId = fileSpecs.Files[0].Dataset.GetSingleValue<string>(DicomTag.PatientID);
                    break;

                case "AccessionNumber":
                    inferenceRequest.InputMetadata.Details.Type = InferenceRequestType.AccessionNumber;
                    inferenceRequest.InputMetadata.Details.AccessionNumber = new List<string>() { fileSpecs.Files[0].Dataset.GetSingleValue<string>(DicomTag.AccessionNumber) };
                    break;

                default:
                    throw new ArgumentException($"invalid ACR request type specified in feature file: {requestType}");
            }
            inferenceRequest.InputResources = new List<RequestInputDataResource>
            {
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.Algorithm,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        Name = "DICOM-RUNNER-TEST",
                        Id = Guid.NewGuid().ToString(),
                    }
                },
                new RequestInputDataResource
                {
                    Interface = InputInterfaceType.DicomWeb,
                    ConnectionDetails = new InputConnectionDetails
                    {
                        Uri = _configuration.OrthancOptions.DicomWebRootInternal,
                        AuthId = _configuration.OrthancOptions.GetBase64EncodedAuthHeader(),
                        AuthType = ConnectionAuthType.Basic
                    }
                }
            };

            if(!inferenceRequest.IsValid(out var details))
            {
                _outputHelper.WriteLine($"Validation error: {details}.");
                throw new Exception(details);
            }
            _scenarioContext[KeyInferenceRequest] = inferenceRequest;
        }

        [When(@"the ACR API request is sent")]
        public async Task WhenTheACRAPIRequestIsSentTo()
        {
            _outputHelper.WriteLine($"Sending inference request...");
            await _informaticsGatewayClient.Inference.NewInferenceRequest(_scenarioContext[KeyInferenceRequest] as InferenceRequest, CancellationToken.None);
        }

        [Then(@"a workflow requests sent to the message broker")]
        public void ThenAWorkflowRequestsSentToTheMessageBroker()
        {
            var inferenceRequest = _scenarioContext[KeyInferenceRequest] as InferenceRequest;
            _rabbitMqHooks.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            var fileSpecs = _scenarioContext[KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;

            messages.Should().NotBeNullOrEmpty().And.HaveCount(WorkflowStudyCount);
            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be(fileSpecs.FileCount);
                request.Workflows.Should().Equal(inferenceRequest.Application.Id);
            }
        }

        [Then(@"a study is uploaded to the storage service")]
        public async Task ThenAStudyIsUploadedToTheStorageService()
        {
            var minioClient = new MinioClient(_configuration.StorageServiceOptions.Endpoint, _configuration.StorageServiceOptions.AccessKey, _configuration.StorageServiceOptions.AccessToken);

            var dicomSizes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;
            _rabbitMqHooks.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            messages.Should().NotBeNullOrEmpty();

            foreach (var message in messages)
            {
                var request = message.ConvertTo<WorkflowRequestEvent>();
                foreach (var file in request.Payload)
                {
                    var dicomValidationKey = string.Empty;
                    await minioClient.GetObjectAsync(request.Bucket, $"{request.PayloadId}/{file.Path}", (stream) =>
                    {
                        using var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        var dicomFile = DicomFile.Open(memoryStream);
                        dicomValidationKey = dicomFile.GenerateFileName();
                        dicomSizes.Should().ContainKey(dicomValidationKey).WhoseValue.Should().Be(dicomFile.CalculateHash());
                    });

                    await minioClient.GetObjectAsync(request.Bucket, $"{request.PayloadId}/{file.Metadata}", (stream) =>
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

        [Then(@"the temporary data directory is cleared")]
        public void ThenTheTemporaryDataDirectoryIsCleared()
        {
            var inferenceRequest = _scenarioContext[KeyInferenceRequest] as InferenceRequest;
            var dataDir = Path.Combine(_configuration.InformaticsGatewayOptions.TemporaryDataStore, inferenceRequest.TransactionId);
            _outputHelper.WriteLine($"Validating temporary data dir {dataDir}");
            Policy
                .Handle<AssertionFailedException>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromMilliseconds(150 * retryAttempt), (exception, retryCount, context) =>
                {
                    _outputHelper.WriteLine("Exception 'validating temporary data directory': {0}", exception.Message);
                })
                .Execute(() =>
                {
                    if (Directory.Exists(dataDir))
                    {
                        var files = Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories);
                        files.Length.Should().Be(0);
                    }
                });
        }
    }
}
