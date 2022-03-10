// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Text;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Monai.Deploy.InformaticsGateway.MessageBroker.RabbitMq
{
    public class RabbitMqMessageSubscriberService : IMessageBrokerSubscriberService, IDisposable
    {
        private readonly ILogger<RabbitMqMessageSubscriberService> _logger;
        private readonly string _endpoint;
        private readonly string _virtualHost;
        private readonly string _exchange;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private bool _disposedValue;

        public string Name => "Rabbit MQ Subscriber";

        public RabbitMqMessageSubscriberService(IOptions<InformaticsGatewayConfiguration> options,
                                                ILogger<RabbitMqMessageSubscriberService> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var configuration = options.Value.Messaging;

            ValidateConfiguration(configuration);
            _endpoint = configuration.SubscriberSettings[ConfigurationKeys.EndPoint];
            var username = configuration.SubscriberSettings[ConfigurationKeys.Username];
            var password = configuration.SubscriberSettings[ConfigurationKeys.Password];
            _virtualHost = configuration.SubscriberSettings[ConfigurationKeys.VirtualHost];
            _exchange = configuration.SubscriberSettings[ConfigurationKeys.Exchange];

            var connectionFactory = new ConnectionFactory()
            {
                HostName = _endpoint,
                UserName = username,
                Password = password,
                VirtualHost = _virtualHost
            };

            _logger.Log(LogLevel.Information, $"{Name} connecting to {_endpoint}/{_virtualHost}");
            _connection = connectionFactory.CreateConnection();
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
            Guard.Against.Null(messageReceivedCallback, nameof(messageReceivedCallback));

            var queueDeclareResult = _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueDeclareResult.QueueName, _exchange, topic);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, eventArgs) =>
            {
                using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object>
                {
                    { "MessageId", eventArgs.BasicProperties.MessageId },
                    { "ApplicationId", eventArgs.BasicProperties.AppId }
                });

                _logger.Log(LogLevel.Information, $"Message received from queue {queueDeclareResult.QueueName} for {topic}.");

                var messageReceivedEventArgs = new MessageReceivedEventArgs(
                 new Message(
                     body: eventArgs.Body.ToArray(),
                     bodyDescription: topic,
                     messageId: eventArgs.BasicProperties.MessageId,
                     applicationId: eventArgs.BasicProperties.AppId,
                     contentType: eventArgs.BasicProperties.ContentType,
                     correlationId: eventArgs.BasicProperties.CorrelationId,
                     creationDateTime: DateTime.Parse(Encoding.UTF8.GetString((byte[])eventArgs.BasicProperties.Headers["CreationDateTime"]), CultureInfo.InvariantCulture),
                     deliveryTag: eventArgs.DeliveryTag.ToString(CultureInfo.InvariantCulture)),
                 new CancellationToken());

                messageReceivedCallback(messageReceivedEventArgs);
            };
            _channel.BasicQos(0, prefetchCount, false);
            _channel.BasicConsume(queueDeclareResult.QueueName, false, consumer);
            _logger.Log(LogLevel.Information, $"Listening for messages from {_endpoint}/{_virtualHost}. Exchange={_exchange}, Queue={queueDeclareResult.QueueName}, Routing Key={topic}");
        }

        public void Acknowledge(MessageBase message)
        {
            Guard.Against.Null(message, nameof(message));

            _logger.Log(LogLevel.Information, $"Sending message acknowledgement for message {message.MessageId}");
            _channel.BasicAck(ulong.Parse(message.DeliveryTag, CultureInfo.InvariantCulture), multiple: false);
            _logger.Log(LogLevel.Information, $"Ackowledge sent for message {message.MessageId}");
        }

        public void Reject(MessageBase message)
        {
            Guard.Against.Null(message, nameof(message));

            _logger.Log(LogLevel.Information, $"Sending nack message {message.MessageId} and requeuing.");
            _channel.BasicNack(ulong.Parse(message.DeliveryTag, CultureInfo.InvariantCulture), multiple: false, requeue: true);
            _logger.Log(LogLevel.Information, $"Nack message sent for message {message.MessageId}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
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
