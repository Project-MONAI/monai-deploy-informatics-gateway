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

using System.Net.Sockets;
using System.Text;
using Ardalis.GuardClauses;
using Minio;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.InformaticsGateway.Integration.Test.Hooks;
using Monai.Deploy.Messaging.Events;
using Monai.Deploy.Messaging.Messages;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.StepDefinitions
{
    [Binding]
    [CollectionDefinition("SpecFlowNonParallelizableFeatures", DisableParallelization = true)]
    public class HealthLevel7Definitions
    {
        internal static readonly TimeSpan WaitTimeSpan = TimeSpan.FromMinutes(2);
        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly RabbitMqHooks _rabbitMqHooks;
        private readonly Dictionary<string, HL7.Dotnetcore.Message> _input;
        private readonly Dictionary<string, string> _output;

        public HealthLevel7Definitions(
            FeatureContext featureContext,
            ScenarioContext scenarioContext,
            ISpecFlowOutputHelper outputHelper,
            Configurations configuration,
            RabbitMqHooks rabbitMqHooks)
        {
            _featureContext = featureContext ?? throw new ArgumentNullException(nameof(featureContext));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _rabbitMqHooks = rabbitMqHooks ?? throw new ArgumentNullException(nameof(rabbitMqHooks));

            _input = new Dictionary<string, HL7.Dotnetcore.Message>();
            _output = new Dictionary<string, string>();
        }

        [Given(@"HL7 messages in version (.*)")]
        public async Task GivenHl7MessagesInVersionX(string version)
        {
            Guard.Against.NullOrWhiteSpace(version, nameof(version));

            var files = Directory.GetFiles($"data/hl7/{version}");

            foreach (var file in files)
            {
                var text = await File.ReadAllTextAsync(file);
                var message = new HL7.Dotnetcore.Message(text);
                message.ParseMessage();
                message.SetValue("MSH.10", file);
                _input[file] = message;
            }
            _rabbitMqHooks.SetupMessageHandle(1);
        }

        [When(@"the message are sent to Informatics Gateway")]
        public async Task WhenTheMessagesAreSentToInformaticsGateway()
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(_configuration.InformaticsGatewayOptions.Host, _configuration.InformaticsGatewayOptions.Hl7Port);
            var networkStream = tcpClient.GetStream();
            foreach (var file in _input.Keys)
            {
                _outputHelper.WriteLine($"Sending file {file}...");
                var data = _input[file].GetMLLP();
                await networkStream.WriteAsync(data, 0, data.Length);
                var buffer = new byte[1048576];
                var responseData = string.Empty;
                do
                {
                    if (await networkStream.ReadAsync(buffer, 0, buffer.Length) == 0)
                    {
                        break;
                    }

                    responseData = Encoding.UTF8.GetString(buffer.ToArray());

                    var startIndex = responseData.IndexOf((char)0x0B);
                    if (startIndex >= 0)
                    {
                        var endIndex = responseData.IndexOf((char)0x1C);

                        if (endIndex > startIndex)
                        {
                            var messageStartIndex = startIndex + 1;
                            var messageEndIndex = endIndex + 1;
                            responseData = responseData.Substring(messageStartIndex, endIndex - messageStartIndex);
                            break;
                        }
                    }
                } while (true);
                _output[file] = responseData;
            }
            tcpClient.Close();
        }

        [When(@"the message are sent to Informatics Gateway in one batch")]
        public async Task WhenTheMessagesAreSentToInformaticsGatewayInOneBatch()
        {
            var messages = new List<byte>();
            foreach (var file in _input.Keys)
            {
                _outputHelper.WriteLine($"Sending file {file}...");
                var data = _input[file].GetMLLP();
                messages.AddRange(data);
            }

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(_configuration.InformaticsGatewayOptions.Host, _configuration.InformaticsGatewayOptions.Hl7Port);
            var networkStream = tcpClient.GetStream();
            await networkStream.WriteAsync(messages.ToArray(), 0, messages.Count);
            var buffer = new byte[512];
            var responseData = string.Empty;

            do
            {
                if (await networkStream.ReadAsync(buffer, 0, buffer.Length) == 0)
                {
                    break;
                }

                responseData = Encoding.UTF8.GetString(buffer.ToArray());
                var rawHl7Messages = HL7.Dotnetcore.MessageHelper.ExtractMessages(responseData);

                foreach (var message in rawHl7Messages)
                {
                    var hl7Message = new HL7.Dotnetcore.Message(message);
                    hl7Message.ParseMessage();
                    var segment = hl7Message.DefaultSegment("MSH");
                    _output[segment.Fields(10).Value] = message;
                }

                if (_output.Count == _input.Count)
                {
                    break;
                }
            } while (true);
            tcpClient.Close();
        }

        [Then(@"acknowledgement are received")]
        public void ThenAcknowledgementAreReceived()
        {
            foreach (var file in _output.Keys)
            {
                _outputHelper.WriteLine($"Verifying acknowledgement for {file}...");
                var message = new HL7.Dotnetcore.Message(_output[file]);
                message.ParseMessage();
                var segment = message.DefaultSegment("MSH");
                _outputHelper.WriteLine($"ACK Value= {segment.Value}...");
                segment.Fields(9).Value.Should().Be("ACK");
            }
        }

        [Then(@"a workflow requests sent to message broker")]
        public void ThenAWorkflowRequestIsSentToMessageBroker()
        {
            _rabbitMqHooks.MessageWaitHandle.Wait(WaitTimeSpan).Should().BeTrue();
        }

        [Then(@"messages are uploaded to storage service")]
        public async Task ThenMessageAreUploadedToStorageService()
        {
            var messages = _scenarioContext[RabbitMqHooks.ScenarioContextKey] as IList<Message>;
            messages.Should().NotBeNullOrEmpty().And.HaveCount(1);
            var message = messages.First();
            message.ApplicationId.Should().Be(MessageBrokerConfiguration.InformaticsGatewayApplicationId);
            var request = message.ConvertTo<WorkflowRequestEvent>();
            request.Should().NotBeNull();
            request.FileCount.Should().Be(_input.Count);

            var minioClient = new MinioClient()
                .WithEndpoint(_configuration.StorageServiceOptions.Endpoint)
                .WithCredentials(_configuration.StorageServiceOptions.AccessKey, _configuration.StorageServiceOptions.AccessToken);

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
                var retryCount = 0;
                var matchFound = false;
            RetryVerifyFileUpload:
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
                        hl7Message.ParseMessage();

                        foreach (var key in _input.Keys)
                        {
                            if (hl7Message.HL7Message.Equals(_input[key].SerializeMessage(true)))
                            {
                                matchFound = true;
                                break;
                            }
                        }

                    });
                await minioClient.GetObjectAsync(getObjectArgs);
                if (retryCount++ < 3 && !matchFound)
                {
                    goto RetryVerifyFileUpload;
                }
                matchFound.Should().BeTrue();
            }
        }
    }
}
