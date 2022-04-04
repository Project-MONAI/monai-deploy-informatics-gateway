﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Text;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.MessageBroker.Common;
using Monai.Deploy.MessageBroker.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Monai.Deploy.MessageBroker.RabbitMq
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

        public RabbitMqMessageSubscriberService(IOptions<MessageBrokerServiceConfiguration> options,
                                                ILogger<RabbitMqMessageSubscriberService> logger)
        {
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var configuration = options.Value;
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

            _logger.ConnectingToRabbitMq(Name, _endpoint, _virtualHost);
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        private void ValidateConfiguration(MessageBrokerServiceConfiguration configuration)
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
                using var loggerScope = _logger.BeginScope(string.Format(CultureInfo.InvariantCulture, Log.LoggingScopeMessageApplication, eventArgs.BasicProperties.MessageId, eventArgs.BasicProperties.AppId));

                _logger.MessageReceivedFromQueue(queueDeclareResult.QueueName, topic);

                var messageReceivedEventArgs = new MessageReceivedEventArgs(
                 new Message(
                     body: eventArgs.Body.ToArray(),
                     messageDescription: topic,
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
            _logger.SubscribeToRabbitMqQueue(_endpoint, _virtualHost, _exchange, queueDeclareResult.QueueName, topic);
        }

        public void Acknowledge(MessageBase message)
        {
            Guard.Against.Null(message, nameof(message));

            _logger.SendingAcknowledgement(message.MessageId);
            _channel.BasicAck(ulong.Parse(message.DeliveryTag, CultureInfo.InvariantCulture), multiple: false);
            _logger.AcknowledgementSent(message.MessageId);
        }

        public void Reject(MessageBase message)
        {
            Guard.Against.Null(message, nameof(message));

            _logger.SendingNAcknowledgement(message.MessageId);
            _channel.BasicNack(ulong.Parse(message.DeliveryTag, CultureInfo.InvariantCulture), multiple: false, requeue: true);
            _logger.NAcknowledgementSent(message.MessageId);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _connection is not null)
                {
                    _logger.ClosingConnection();
                    _connection.Close();
                    _connection.Dispose();
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