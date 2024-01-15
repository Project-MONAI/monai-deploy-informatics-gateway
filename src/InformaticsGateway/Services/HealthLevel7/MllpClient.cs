/*
 * Copyright 2022-2023 MONAI Consortium
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;

namespace Monai.Deploy.InformaticsGateway.Api.Mllp
{
    internal sealed class MllpClient : IMllpClient
    {
        private readonly ITcpClientAdapter _client;
        private readonly Hl7Configuration _configurations;
        private readonly ILogger<MllpClient> _logger;
        private readonly List<Exception> _exceptions;
        private readonly List<Message> _messages;
        private readonly IDisposable _loggerScope;
        private bool _disposedValue;

        public Guid ClientId { get; }

        public string ClientIp
        {
            get { return _client.RemoteEndPoint.ToString() ?? string.Empty; }
        }

        public MllpClient(ITcpClientAdapter client, Hl7Configuration configurations, ILogger<MllpClient> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ClientId = Guid.NewGuid();
            _exceptions = new List<Exception>();
            _messages = new List<Message>();

            _loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "Endpoint", _client.RemoteEndPoint }, { "CorrelationId", ClientId } });
        }

        public async Task Start(Func<IMllpClient, MllpClientResult, Task> onDisconnect, CancellationToken cancellationToken)
        {
            using var clientStream = _client.GetStream();
            clientStream.ReadTimeout = _configurations.ClientTimeoutMilliseconds;
            clientStream.WriteTimeout = _configurations.ClientTimeoutMilliseconds;

            var messages = await ReceiveData(clientStream, cancellationToken).ConfigureAwait(false);
            _messages.AddRange(messages);

            if (onDisconnect is not null)
            {
                await onDisconnect(this, new MllpClientResult(_messages, _exceptions.Count > 0 ? new AggregateException(_exceptions) : null)).ConfigureAwait(false);
            }
        }

        private async Task<IList<Message>> ReceiveData(INetworkStream clientStream, CancellationToken cancellationToken)
        {
            Guard.Against.Null(clientStream, nameof(clientStream));

            var data = string.Empty;
            var messages = new List<Message>();
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            while (true)
            {
                var messageBuffer = new Memory<byte>(new byte[_configurations.BufferSize]);
                int bytesRead;
                try
                {
                    _logger.HL7ReadingMessage();
                    linkedCancellationTokenSource.CancelAfter(_configurations.ClientTimeoutMilliseconds);
                    bytesRead = await clientStream.ReadAsync(messageBuffer, linkedCancellationTokenSource.Token).ConfigureAwait(false);
                    _logger.Hl7MessageBytesRead(bytesRead);
                    if (!linkedCancellationTokenSource.TryReset())
                    {
                        linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ExceptionReadingClientStream(ex);
                    _exceptions.Add(ex);
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                data += Encoding.UTF8.GetString(messageBuffer.ToArray());

                do
                {
                    var startIndex = data.IndexOf(Resources.AsciiVT);
                    if (startIndex >= 0)
                    {
                        var endIndex = data.IndexOf(Resources.AsciiFS);

                        if (endIndex > startIndex)
                        {
                            if (!CreateMessage(startIndex, endIndex, ref data, out var message))
                            {
                                break;
                            }
                            else
                            {
                                await SendAcknowledgment(clientStream, message, cancellationToken).ConfigureAwait(false);
                                messages.Add(message);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        data = string.Empty;
                        break;
                    }
                } while (true);
            }
            linkedCancellationTokenSource.Dispose();
            return messages;
        }

        private async Task SendAcknowledgment(INetworkStream clientStream, Message message, CancellationToken cancellationToken)
        {
            Guard.Against.Null(clientStream, nameof(clientStream));
            Guard.Against.Null(message, nameof(message));

            if (!_configurations.SendAcknowledgment)
            {
                return;
            }

            if (ShouldSendAcknowledgment(message))
            {
                var ackMessage = message.GetACK(true);
                if (ackMessage is null)
                {
                    _logger.ErrorGeneratingHl7Acknowledgment(new Exception(), message.HL7Message);
                    return;
                }
                var ackData = new ReadOnlyMemory<byte>(ackMessage.GetMLLP());
                try
                {
                    await clientStream.WriteAsync(ackData, cancellationToken).ConfigureAwait(false);
                    await clientStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    _logger.AcknowledgmentSent(ackMessage.HL7Message, ackData.Length);
                }
                catch (Exception ex)
                {
                    _logger.ErrorSendingHl7Acknowledgment(ex);
                    _exceptions.Add(ex);
                }
            }
        }

        private bool ShouldSendAcknowledgment(Message message)
        {
            Guard.Against.Null(message, nameof(message));
            try
            {
                var value = message.DefaultSegment(Resources.MessageHeaderSegment).Fields(Resources.AcceptAcknowledgementType);
                if (value is null || string.IsNullOrWhiteSpace(value.Value))
                {
                    return true;
                }

                _logger.AcknowledgmentType(value.Value);

                return value.Value switch
                {
                    Resources.AcknowledgmentTypeNever => false,
                    Resources.AcknowledgmentTypeError => _exceptions.Any(),
                    Resources.AcknowledgmentTypeSuccessful => !_exceptions.Any(),
                    _ => true,
                };
            }
            catch (Exception ex)
            {
                _logger.MissingFieldInHL7Message(Resources.MessageHeaderSegment, Resources.AcceptAcknowledgementType, ex);
                _exceptions.Add(ex);
                return true;
            }
        }

        private bool CreateMessage(int startIndex, int endIndex, ref string data, out Message message)
        {
            var messageStartIndex = startIndex + 1;
            var messageEndIndex = endIndex + 1;
            try
            {
                var text = data.Substring(messageStartIndex, endIndex - messageStartIndex);
                _logger.Hl7GenerateMessage(text.Length, text);
                message = new Message(text);
                message.ParseMessage(true);
                data = data.Length > endIndex ? data.Substring(messageEndIndex) : string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                message = new();
                _logger.ErrorParsingHl7Message(ex);
                _exceptions.Add(ex);
                return false;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _client.Close();
                    _client.Dispose();
                    _loggerScope.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
