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
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal class Hl7DataClient : IDataClient
    {
        private readonly Configurations _configurations;
        private readonly InformaticsGatewayConfiguration _options;
        private readonly ISpecFlowOutputHelper _outputHelper;

        public Hl7DataClient(Configurations configurations, InformaticsGatewayConfiguration options, ISpecFlowOutputHelper outputHelper)
        {
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public async Task SendAsync(DataProvider dataProvider, params object[] args)
        {
            Guard.Against.Null(dataProvider);
            Guard.Against.NullOrEmpty(args);

            var batch = (bool)args[0];

            if (batch)
            {
                await SendBatchAsync(dataProvider, args);
            }
            else
            {
                await SendOneAsync(dataProvider, args);
            }
        }

        private async Task SendOneAsync(DataProvider dataProvider, params object[] args)
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(_configurations.InformaticsGatewayOptions.Host, _options.Hl7.Port);
            var networkStream = tcpClient.GetStream();
            foreach (var file in dataProvider.HL7Specs.Files.Keys)
            {
                _outputHelper.WriteLine($"Sending file {file}...");
                var data = dataProvider.HL7Specs.Files[file].GetMLLP();
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
                dataProvider.HL7Specs.Responses[file] = responseData;
            }
            tcpClient.Close();
        }

        private async Task SendBatchAsync(DataProvider dataProvider, params object[] args)
        {
            var messages = new List<byte>();
            foreach (var file in dataProvider.HL7Specs.Files.Keys)
            {
                _outputHelper.WriteLine($"Sending file {file}...");
                var data = dataProvider.HL7Specs.Files[file].GetMLLP();
                messages.AddRange(data);
            }

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(_configurations.InformaticsGatewayOptions.Host, _options.Hl7.Port);
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
                    dataProvider.HL7Specs.Responses[segment.Fields(10).Value] = message;
                }

                if (dataProvider.HL7Specs.Responses.Count == dataProvider.HL7Specs.Files.Count)
                {
                    break;
                }
            } while (true);
            tcpClient.Close();
        }
    }
}
