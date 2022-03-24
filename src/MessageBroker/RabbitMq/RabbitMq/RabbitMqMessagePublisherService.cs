// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.MessageBroker.Common;
using Monai.Deploy.MessageBroker.Messages;
using RabbitMQ.Client;

namespace Monai.Deploy.MessageBroker.RabbitMq
{
    public class RabbitMqMessagePublisherService : IMessageBrokerPublisherService, IDisposable
    {
        private readonly ILogger<RabbitMqMessagePublisherService> _logger;
        private readonly string _endpoint;
        private readonly string _virtualHost;
        private readonly string _exchange;
        private readonly IConnection _connection;
        private bool _disposedValue;

        public string Name => "Rabbit MQ Publisher";

        public RabbitMqMessagePublisherService(IOptions<MessageBrokerServiceConfiguration> options,
                                               ILogger<RabbitMqMessagePublisherService> logger)
        {
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var configuration = options.Value;
            ValidateConfiguration(configuration);
            _endpoint = configuration.PublisherSettings[ConfigurationKeys.EndPoint];
            var username = configuration.PublisherSettings[ConfigurationKeys.Username];
            var password = configuration.PublisherSettings[ConfigurationKeys.Password];
            _virtualHost = configuration.SubscriberSettings[ConfigurationKeys.VirtualHost];
            _exchange = configuration.SubscriberSettings[ConfigurationKeys.Exchange];

            var connectionFactory = new ConnectionFactory()
            {
                HostName = _endpoint,
                UserName = username,
                Password = password,
                VirtualHost = _virtualHost
            };
            _connection = connectionFactory.CreateConnection();
        }

        private void ValidateConfiguration(MessageBrokerServiceConfiguration configuration)
        {
            Guard.Against.Null(configuration, nameof(configuration));
            Guard.Against.Null(configuration.PublisherSettings, nameof(configuration.PublisherSettings));

            foreach (var key in ConfigurationKeys.PublisherRequiredKeys)
            {
                if (!configuration.PublisherSettings.ContainsKey(key))
                {
                    throw new ConfigurationException($"{Name} is missing configuration for {key}.");
                }
            }
        }

        public Task Publish(string topic, Message message)
        {
            Guard.Against.NullOrWhiteSpace(topic, nameof(topic));
            Guard.Against.Null(message, nameof(message));

            using var loggerScope = _logger.BeginScope(string.Format(CultureInfo.InvariantCulture, Log.LoggingScopeMessageApplication, message.MessageId, message.ApplicationId));

            _logger.PublshingRabbitMq(_endpoint, _virtualHost, _exchange, topic);

            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(_exchange, ExchangeType.Topic);

            var propertiesDictionary = new Dictionary<string, object>
            {
                { "CreationDateTime", message.CreationDateTime.ToString("o") }
            };

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = message.ContentType;
            properties.MessageId = message.MessageId;
            properties.AppId = message.ApplicationId;
            properties.CorrelationId = message.CorrelationId;
            properties.DeliveryMode = 2;

            properties.Headers = propertiesDictionary;
            channel.BasicPublish(exchange: _exchange,
                                 routingKey: topic,
                                 basicProperties: properties,
                                 body: message.Body);

            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _connection != null)
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
