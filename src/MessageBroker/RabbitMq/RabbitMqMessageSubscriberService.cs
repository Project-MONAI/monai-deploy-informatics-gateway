// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Monai.Deploy.InformaticsGateway.MessageBroker.RabbitMq
{
    public class RabbitMqMessageSubscriberService : IMessageBrokerSubscriberService, IDisposable
    {
        private readonly ILogger<RabbitMqMessageSubscriberService> _logger;
        private readonly MessageBrokerConfiguration _configuration;
        private readonly string _endpoint;
        private readonly string _username;
        private readonly string _password;
        private readonly string _virtualHost;
        private readonly string _exchange;
        private readonly ConnectionFactory _connectionFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private bool disposedValue;

        public string Name => "Rabbit MQ Subscriber";

        public RabbitMqMessageSubscriberService(IOptions<InformaticsGatewayConfiguration> options,
                                                ILogger<RabbitMqMessageSubscriberService> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = options.Value.Messaging;

            ValidateConfiguration(_configuration);
            _endpoint = _configuration.SubscriberSettings[ConfigurationKeys.EndPoint];
            _username = _configuration.SubscriberSettings[ConfigurationKeys.Username];
            _password = _configuration.SubscriberSettings[ConfigurationKeys.Password];
            _virtualHost = _configuration.SubscriberSettings[ConfigurationKeys.VirtualHost];
            _exchange = _configuration.SubscriberSettings[ConfigurationKeys.Exchange];

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _endpoint,
                UserName = _username,
                Password = _password,
                VirtualHost = _virtualHost
            };

            _logger.Log(LogLevel.Information, $"{Name} connecting to {_endpoint}/{_virtualHost}");
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        private void ValidateConfiguration(MessageBrokerConfiguration configuration)
        {
            Guard.Against.Null(configuration, nameof(configuration));
            Guard.Against.Null(configuration.SubscriberSettings, nameof(configuration.SubscriberSettings));

            foreach (var key in ConfigurationKeys.SubscriberRequiredKeys)
            {
                if (!configuration.SubscriberSettings.ContainsKey(key))
                {
                    throw new ConfigurationException($"{Name} is missing configuration for {key}.");
                }
            }
        }

        public void Subscribe(string topic, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount = 0)
        {
            Guard.Against.NullOrWhiteSpace(topic, nameof(topic));
            Guard.Against.NullOrWhiteSpace(queue, nameof(queue));
            Guard.Against.Null(messageReceivedCallback, nameof(messageReceivedCallback));

            _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue, _exchange, topic);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, eventArgs) =>
            {
                using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object>
                {
                    { "MessageId", eventArgs.BasicProperties.MessageId },
                    { "ApplicationId", eventArgs.BasicProperties.AppId }
                });

                _logger.Log(LogLevel.Information, $"Message received from queue {queue} for {topic}.");

                var messageReceivedEventArgs = new MessageReceivedEventArgs(
                 new Message(
                     body: eventArgs.Body.ToArray(),
                     bodyDescription: topic,
                     messageId: eventArgs.BasicProperties.MessageId,
                     applicationId: eventArgs.BasicProperties.AppId,
                     contentType: eventArgs.BasicProperties.ContentType,
                     correlationId: eventArgs.BasicProperties.CorrelationId,
                     creationDateTime: DateTime.Parse(Encoding.UTF8.GetString((byte[])eventArgs.BasicProperties.Headers["CreationDateTime"])),
                     deliveryTag: eventArgs.DeliveryTag.ToString()),
                 new CancellationToken());

                messageReceivedCallback(messageReceivedEventArgs);
            };
            _channel.BasicQos(0, prefetchCount, false);
            _channel.BasicConsume(queue, false, consumer);
            _logger.Log(LogLevel.Information, $"Listening for messages from {_endpoint}/{_virtualHost}. Exchange={_exchange}, Queue={queue}, Routing Key={topic}");
        }

        public void Acknowledge(MessageBase message)
        {
            Guard.Against.Null(message, nameof(message));

            _logger.Log(LogLevel.Information, $"Sending message acknowledgement for message {message.MessageId}");
            _channel.BasicAck(ulong.Parse(message.DeliveryTag), multiple: false);
            _logger.Log(LogLevel.Information, $"Ackowledge sent for message {message.MessageId}");
        }

        public void Reject(MessageBase message)
        {
            Guard.Against.Null(message, nameof(message));

            _logger.Log(LogLevel.Information, $"Sending nack message {message.MessageId} and requeuing.");
            _channel.BasicNack(ulong.Parse(message.DeliveryTag), multiple: false, requeue: true);
            _logger.Log(LogLevel.Information, $"Nack message sent for message {message.MessageId}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_connection is not null)
                    {
                        _logger.Log(LogLevel.Information, $"Closing connection.");
                        _connection.Close();
                        _connection.Dispose();
                    }
                }

                disposedValue = true;
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
