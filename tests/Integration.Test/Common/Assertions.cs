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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Minio;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class Assertions
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public Assertions(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        internal async Task ShouldHaveUploadedDicomDataToMinio(IReadOnlyList<Message> messages, Dictionary<string, string> fileHashes)
        {
            Guard.Against.Null(messages);
            Guard.Against.NullOrEmpty(fileHashes);

            var minioClient = GetMinioClient();

            foreach (var message in messages)
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
                            fileHashes.Should().ContainKey(dicomValidationKey).WhoseValue.Should().Be(dicomFile.CalculateHash());
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

        internal async Task ShouldHaveUploadedFhirDataToMinio(IReadOnlyList<Message> messages, Dictionary<string, string> fhirData)
        {
            Guard.Against.Null(messages);

            var minioClient = GetMinioClient();

            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be(1);

                foreach (var file in request.Payload)
                {
                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(request.Bucket)
                        .WithObject($"{request.PayloadId}/{file.Path}")
                        .WithCallbackStream((stream) =>
                        {
                            using var memoryStream = new MemoryStream();
                            stream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            var data = Encoding.UTF8.GetString(memoryStream.ToArray());
                            data.Should().NotBeNullOrWhiteSpace();

                            var incomingFilename = Path.GetFileName(file.Path);
                            var storedFileKey = fhirData.Keys.FirstOrDefault(p => p.EndsWith(incomingFilename));
                            storedFileKey.Should().NotBeNull();

                            _outputHelper.WriteLine($"Validating file {storedFileKey}...");
                            if (incomingFilename.EndsWith(".json", true, CultureInfo.InvariantCulture))
                            {
                                ValidateJson(fhirData[storedFileKey], data);
                            }
                            else
                            {
                                ValidateXml(fhirData[storedFileKey], data);
                            }
                        });
                    await minioClient.GetObjectAsync(getObjectArgs);
                }
            }
        }

        internal async Task ShouldHaveUploadedHl7ataToMinio(IReadOnlyList<Message> messages)
        {
            Guard.Against.Null(messages);

            var minioClient = GetMinioClient();

            foreach (var message in messages)
            {
                var request = message.ConvertTo<WorkflowRequestEvent>();
                var listOjbectsArgs = new ListObjectsArgs()
                        .WithBucket(request.Bucket)
                        .WithPrefix(request.PayloadId.ToString())
                        .WithRecursive(true);
                var results = minioClient.ListObjectsAsync(listOjbectsArgs);
                results.Subscribe(item =>
                {
                    _outputHelper.WriteLine($"File => {item.Key}...");
                },
                exception =>
                {
                    _outputHelper.WriteLine($"Error listing files {exception.Message}");
                });

                foreach (var file in request.Payload)
                {
                    _outputHelper.WriteLine($"Verifying file => {request.PayloadId}/{file.Path}...");
                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(request.Bucket)
                        .WithObject($"{request.PayloadId}/{file.Path}")
                        .WithCallbackStream((stream) =>
                        {
                            using var memoryStream = new MemoryStream();
                            stream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            var data = Encoding.UTF8.GetString(memoryStream.ToArray());

                            var hl7Message = new HL7.Dotnetcore.Message(data);
                            hl7Message.ParseMessage().Should().BeTrue();
                        });
                    await minioClient.GetObjectAsync(getObjectArgs);
                }
            }
        }

        internal void ShouldHaveCorrectNumberOfWorkflowRequestMessages(DataProvider dataProvider, IReadOnlyList<Message> messages, int count)
        {
            Guard.Against.Null(dataProvider);
            Guard.Against.Null(messages);

            messages.Should().NotBeNullOrEmpty().And.HaveCount(count);
            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be((dataProvider.DicomSpecs.NumberOfExpectedFiles(dataProvider.StudyGrouping)));

                if (dataProvider.Workflows is not null)
                {
                    request.Workflows.Should().Equal(dataProvider.Workflows);
                }
            }
        }
        internal void ShouldHaveCorrectNumberOfWorkflowRequestMessagesAndAcrRequest(DataProvider dataProvider, IReadOnlyList<Message> messages, int count)
        {
            Guard.Against.Null(dataProvider);
            Guard.Against.Null(messages);

            messages.Should().NotBeNullOrEmpty().And.HaveCount(count);

            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be(dataProvider.DicomSpecs.FileCount);
                request.Workflows.Should().Equal(dataProvider.AcrRequest.Application.Id);
            }
        }

        internal void ShouldHaveCorrectNumberOfWorkflowRequestMessagesAndHl7Messages(Hl7Messages hL7Specs, IReadOnlyList<Message> messages, int count)
        {
            Guard.Against.Null(hL7Specs);
            Guard.Against.Null(messages);

            messages.Should().NotBeNullOrEmpty().And.HaveCount(count);

            foreach (var message in messages)
            {
                message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
                var request = message.ConvertTo<WorkflowRequestEvent>();
                request.Should().NotBeNull();
                request.FileCount.Should().Be(hL7Specs.Files.Count);
            }
        }

        private MinioClient GetMinioClient() => new MinioClient()
                    .WithEndpoint(_options.Storage.Settings["endpoint"])
                    .WithCredentials(_options.Storage.Settings["accessKey"], _options.Storage.Settings["accessToken"])
                    .Build();

        private void ValidateXml(string expected, string actual)
        {
            expected = FormatXml(expected);
            expected.Should().Be(actual);
        }

        private string FormatXml(string xml)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
            {
                xmlDocument.Save(xmlWriter);
            }
            return sb.ToString();
        }

        private void ValidateJson(string expected, string actual)
        {
            expected = FormatJson(expected);
            expected.Should().Be(actual);
        }

        private string FormatJson(string expected)
        {
            var jsonDoc = JsonNode.Parse(expected);
            return jsonDoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        internal void ShoulddHaveCorrectNumberOfAckMessages(Dictionary<string, string> responses)
        {
            foreach (var file in responses.Keys)
            {
                _outputHelper.WriteLine($"Verifying acknowledgement for {file}...");
                var message = new HL7.Dotnetcore.Message(responses[file]);
                message.ParseMessage();
                var segment = message.DefaultSegment("MSH");
                _outputHelper.WriteLine($"ACK Value= {segment.Value}...");
                segment.Fields(9).Value.Should().Be("ACK");
            }
        }
    }
}
