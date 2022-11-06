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

using System.Globalization;
using System.Text;
using Ardalis.GuardClauses;
using BoDi;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Minio;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Common;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.Messaging.Events;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class SharedDefinitions
    {
        internal static readonly TimeSpan MessageWaitTimeSpan = TimeSpan.FromMinutes(10);
        internal static readonly string KeyDicomHashes = "DICOM_HASHES";
        internal static readonly string KeyDicomFiles = "DICOM_FILES";
        internal static readonly string KeyCalledAet = "CALLED_AET";
        internal static readonly string KeyDataGrouping = "DICOM_DATA_GROUPING";
        internal static readonly string KeyDimseResponse = "DIMSE_RESPONSE";
        internal static readonly string KeyWorkflows = "WORKFLOWS";
        private readonly InformaticsGatewayConfiguration _informaticsGatewayConfiguration;
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly DicomInstanceGenerator _dicomInstanceGenerator;
        private readonly RabbitMqConsumer _receivedMessages;

        public SharedDefinitions(
            ObjectContainer objectContainer,
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            DicomInstanceGenerator dicomInstanceGenerator)
        {
            if (objectContainer is null)
            {
                throw new ArgumentNullException(nameof(objectContainer));
            }
            _informaticsGatewayConfiguration = objectContainer.Resolve<InformaticsGatewayConfiguration>("InformaticsGatewayConfiguration");
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomInstanceGenerator = dicomInstanceGenerator ?? throw new ArgumentNullException(nameof(dicomInstanceGenerator));
            _receivedMessages = objectContainer.Resolve<RabbitMqConsumer>("WorkflowRequestSubscriber");
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
            _receivedMessages.SetupMessageHandle(fileSpecs.NumberOfExpectedRequests(_scenarioContext[KeyDataGrouping].ToString()));
            _outputHelper.WriteLine($"File specs: {fileSpecs.StudyCount}, {fileSpecs.SeriesPerStudyCount}, {fileSpecs.InstancePerSeries}, {fileSpecs.FileCount}");
        }

        [Then(@"(.*) workflow requests sent to message broker")]
        public void ThenWorkflowRequestSentToMessageBroker(int workflowCount)
        {
            Guard.Against.NegativeOrZero(workflowCount, nameof(workflowCount));

            _receivedMessages.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            var fileSpecs = _scenarioContext[KeyDicomFiles] as DicomInstanceGenerator.StudyGenerationSpecs;

            _receivedMessages.Messages.Should().NotBeNullOrEmpty().And.HaveCount(workflowCount);
            foreach (var message in _receivedMessages.Messages)
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

        [Then(@"studies are uploaded to storage service")]
        public async Task ThenXXFilesUploadedToStorageService()
        {
            var minioClient = new MinioClient()
                .WithEndpoint(_informaticsGatewayConfiguration.Storage.Settings["endpoint"])
                .WithCredentials(_informaticsGatewayConfiguration.Storage.Settings["accessKey"], _informaticsGatewayConfiguration.Storage.Settings["accessToken"])
                .Build();

            var dicomSizes = _scenarioContext[KeyDicomHashes] as Dictionary<string, string>;
            _receivedMessages.MessageWaitHandle.Wait(MessageWaitTimeSpan).Should().BeTrue();
            _receivedMessages.Messages.Should().NotBeNullOrEmpty();

            foreach (var message in _receivedMessages.Messages)
            {
                var request = message.ConvertTo<WorkflowRequestEvent>();
                foreach (var file in request.Payload)
                {
                    var dicomValidationKey = string.Empty;
                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(request.Bucket)
                        .WithObject($"{request.PayloadId}/{file.Path}")
                        .WithCallbackStream((stream) =>
                        {
                            using var memoryStream = new MemoryStream();
                            stream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            var dicomFile = DicomFile.Open(memoryStream);
                            dicomValidationKey = dicomFile.GenerateFileName();
                            dicomSizes.Should().ContainKey(dicomValidationKey).WhoseValue.Should().Be(dicomFile.CalculateHash());
                        });
                    await minioClient.GetObjectAsync(getObjectArgs);

                    var getMetadataObjectArgs = new GetObjectArgs()
                        .WithBucket(request.Bucket)
                        .WithObject($"{request.PayloadId}/{file.Metadata}")
                        .WithCallbackStream((stream) =>
                        {
                            using var memoryStream = new MemoryStream();
                            stream.CopyTo(memoryStream);
                            var json = Encoding.UTF8.GetString(memoryStream.ToArray());

                            var dicomFileFromJson = DicomJson.ConvertJsonToDicom(json);
                            var key = dicomFileFromJson.GenerateFileName();
                            key.Should().Be(dicomValidationKey);
                        });
                    await minioClient.GetObjectAsync(getMetadataObjectArgs);
                }
            }
        }
    }
}
