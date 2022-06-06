// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Text;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using FluentAssertions.Execution;
using Minio;
using Monai.Deploy.InformaticsGateway.Api;
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
    public class SharedDefinitions
    {
        internal static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(3);
        internal static readonly string KeyDicomHashes = "DICOM_HASHES";
        internal static readonly string KeyDicomFiles = "DICOM_FILES";
        internal static readonly string KeyCalledAet = "CALLED_AET";
        internal static readonly string KeyDataGrouping = "DICOM_DATA_GROUPING";
        internal static readonly string KeyDimseResponse = "DIMSE_RESPONSE";
        internal static readonly string KeyWorkflows = "WORKFLOWS";

        protected readonly ScenarioContext _scenarioContext;
        protected readonly ISpecFlowOutputHelper _outputHelper;
        protected readonly Configurations _configuration;
        protected readonly DicomInstanceGenerator _dicomInstanceGenerator;
        protected readonly RabbitMqHooks _rabbitMqHooks;

        public SharedDefinitions(
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

        [Given(@"(.*) (.*) studies")]
        public void GivenNStudies(int studyCount, string modality)
        {
            Guard.Against.NegativeOrZero(studyCount, nameof(studyCount));
            Guard.Against.NullOrWhiteSpace(modality, nameof(modality));

            _outputHelper.WriteLine($"Generating {studyCount} {modality} study");
            _configuration.StudySpecs.ContainsKey(modality).Should().BeTrue();

            var studySpec = _configuration.StudySpecs[modality];
            var patientId = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var fileSpecs = _dicomInstanceGenerator.Generate(patientId, studyCount, modality, studySpec);
            _scenarioContext[KeyDicomFiles] = fileSpecs;
            _rabbitMqHooks.SetupMessageHandle(fileSpecs.NumberOfExpectedRequests(_scenarioContext[KeyDataGrouping].ToString()));
            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount}, {fileSpecs.SeriesPerStudyCount}, {fileSpecs.InstancePerSeries}, {fileSpecs.FileCount}");
        }


        [Then(@"(.*) workflow requests sent to message broker")]
        public void ThenWorkflowRequestSentToMessageBroker(int workflowCount)
        {
            Guard.Against.NegativeOrZero(workflowCount, nameof(workflowCount));

            _rabbitMqHooks.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            var fileSpecs = _scenarioContext[KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;

            messages.Should().NotBeNullOrEmpty().And.HaveCount(workflowCount);
            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be((fileSpecs.NumberOfExpectedFiles(_scenarioContext[KeyDataGrouping].ToString())));

                if (_scenarioContext.ContainsKey(SharedDefinitions.KeyWorkflows) &&
                        _scenarioContext[SharedDefinitions.KeyWorkflows] is string[] workflows)
                {
                    request.Workflows.Should().Equal(workflows);
                }
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
                    var directory = string.Empty;
                    if (_scenarioContext.ContainsKey(KeyCalledAet))
                    {
                        var calledAet = _scenarioContext[KeyCalledAet] as MonaiApplicationEntity;
                        directory = Path.Combine(_configuration.InformaticsGatewayOptions.TemporaryDataStore, calledAet.AeTitle);
                        _outputHelper.WriteLine($"Validating AE Title data dir {directory}");
                    }
                    else
                    {
                        var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
                        messages.Should().NotBeNullOrEmpty();
                        var request = messages.First().ConvertTo<WorkflowRequestEvent>();
                        directory = Path.Combine(_configuration.InformaticsGatewayOptions.TemporaryDataStore, request.PayloadId.ToString());
                        _outputHelper.WriteLine($"Validating data dir {directory}");
                    }

                    if (Directory.Exists(directory))
                    {
                        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                        files.Length.Should().Be(0);
                    }
                });
        }

        [Then(@"studies are uploaded to storage service")]
        public async Task ThenXXFilesUploadedToStorageService()
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
    }
}