// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using HL7.Dotnetcore;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal sealed class MllpClient
    {
        private readonly TcpClient _client;
        private readonly Hl7Configuration _configurations;
        private readonly ILogger<MllpClient> _logger;
        private readonly Guid _connectionId;
        private readonly List<Exception> _exceptions;
        private readonly List<Message> _messages;

        public MllpClient(TcpClient client, Hl7Configuration configurations, ILogger<MllpClient> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connectionId = Guid.NewGuid();
            _exceptions = new List<Exception>();
            _messages = new List<Message>();

            _logger.BeginScope(new LoggingDataDictionary<string, object> { { "End point", _client.Client.RemoteEndPoint }, { "CorrelationId", _connectionId } });
        }

        internal async Task Start(Action<TcpClient, MllpClientResult> onDisconnect, CancellationToken cancellationToken)
        {
            using var clientStream = _client.GetStream();
            clientStream.ReadTimeout = _configurations.ClientTimeoutMilliseconds;
            clientStream.WriteTimeout = _configurations.ClientTimeoutMilliseconds;

            var messages = await ReceiveData(clientStream, cancellationToken).ConfigureAwait(false);
            _messages.AddRange(messages);

            if (onDisconnect is not null)
            {
                onDisconnect(_client, new MllpClientResult(_messages, _exceptions.Count > 0 ? new AggregateException(_exceptions) : null));
            }
        }

        private async Task SendAcknowledgment(NetworkStream clientStream, Message message, CancellationToken cancellationToken)
        {
            Guard.Against.Null(clientStream, nameof(clientStream));
            Guard.Against.Null(message, nameof(message));

            if (!_configurations.SendAcknowledgment)
            {
                return;
            }

            if (ShouldSendAcknowledgment(message))
            {
                var ackMessage = message.GetACK();
                var ackData = new ReadOnlyMemory<byte>(ackMessage.GetMLLP());
                try
                {
                    await clientStream.WriteAsync(ackData, cancellationToken).ConfigureAwait(false);
                    await clientStream.FlushAsync(cancellationToken);
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
                if (value is null)
                {
                    return true;
                }
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

        private async Task<IList<Message>> ReceiveData(NetworkStream clientStream, CancellationToken cancellationToken)
        {
            Guard.Against.Null(clientStream, nameof(clientStream));

            var messageBuffer = new Memory<byte>(new byte[_configurations.BufferSize]);
            int bytesRead;
            var data = string.Empty;
            var messages = new List<Message>();

            while (true)
            {
                try
                {
                    bytesRead = await clientStream.ReadAsync(messageBuffer, cancellationToken).ConfigureAwait(false);
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
                }
            }
            return messages;
        }

        private bool CreateMessage(int startIndex, int endIndex, ref string data, out Message message)
        {
            var messageStartIndex = startIndex + 1;
            var messageEndIndex = endIndex + 1;
            try
            {
                message = new Message(data.Substring(messageStartIndex, endIndex - messageStartIndex));
                message.ParseMessage();
                data = data.Length > endIndex ? data.Substring(messageEndIndex) : string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                message = null;
                _logger.ErrorParsingHl7Message(ex);
                _exceptions.Add(ex);
                return false;
            }
        }
    }
}
