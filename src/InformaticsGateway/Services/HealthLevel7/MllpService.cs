/*
 * Copyright 2023 MONAI Consortium
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Mllp;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal class MllpService : IMllpService
    {

        private readonly ILogger<MllpService> _logger;
        private readonly InformaticsGatewayConfiguration _configuration;

        public MllpService(ILogger<MllpService> logger, IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration)); ;
        }

        public async Task SendMllp(IPAddress address, int port, string hl7Message, CancellationToken cancellationToken)
        {
            try
            {
                var body = $"{Resources.AsciiVT}{hl7Message}{Resources.AsciiFS}{Resources.AcsiiCR}";
                var sendMessageByteBuffer = Encoding.UTF8.GetBytes(body);
                await WriteMessage(sendMessageByteBuffer, address, port).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
                _logger.Hl7AckMissingStartOrEndCharacters();
                throw new Hl7SendException("ACK missing start or end characters");
            }
            catch (Exception ex)
            {
                _logger.Hl7SendException(ex);
                throw new Hl7SendException($"Send exception: {ex.Message}");
            }
        }

        private async Task WriteMessage(byte[] sendMessageByteBuffer, IPAddress address, int port)
        {

            using var tcpClient = new TcpClient();

            tcpClient.Connect(address, port);

            var networkStream = new NetworkStream(tcpClient.Client);

            if (networkStream.CanWrite)
            {
                networkStream.Write(sendMessageByteBuffer, 0, sendMessageByteBuffer.Length);
                networkStream.Flush();
            }
            else
            {
                _logger.Hl7ClientStreamNotWritable();
                throw new Hl7SendException("Client stream not writable");
            }

            _logger.Hl7MessageSent(Encoding.UTF8.GetString(sendMessageByteBuffer));

            await EnsureAck(networkStream).ConfigureAwait(false);
        }

        private async Task EnsureAck(NetworkStream networkStream)
        {
            using var s_cts = new CancellationTokenSource();
            s_cts.CancelAfter(_configuration.Hl7.ClientTimeoutMilliseconds);
            var buffer = new byte[2048];

            // get the SentHl7Message
            networkStream.ReadTimeout = 5000;
            networkStream.WriteTimeout = 5000;

            // wait for responce
            while (!s_cts.IsCancellationRequested && networkStream.DataAvailable == false)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }

            var bytesRead = await networkStream.ReadAsync(buffer).ConfigureAwait(false);

            if (bytesRead == 0 || s_cts.IsCancellationRequested)
            {
                throw new Hl7SendException("ACK message contains no ACK!");
            }

            var _rawHl7Messages = MessageHelper.ExtractMessages(Encoding.UTF8.GetString(buffer));
            foreach (var message in _rawHl7Messages)
            {
                var hl7Message = new Message(message);
                hl7Message.ParseMessage(false);
                if (hl7Message.MessageStructure == "ACK")
                {
                    return;
                }
            }
            throw new Hl7SendException("ACK message contains no ACK!");
        }
    }
}
