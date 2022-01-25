// Copyright 2022 MONAI Consortium
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

namespace Monai.Deploy.InformaticsGateway.MessageBroker.RabbitMq
{
    public class RabbitMqMessagePublisherService : IMessageBrokerPublisherService, IDisposable
    {
        private readonly ILogger<RabbitMqMessagePublisherService> _logger;
        private readonly MessageBrokerConfiguration _configuration;
        private readonly string _endpoint;
        private readonly string _username;
        private readonly string _password;
        private readonly string _virtualHost;
        private readonly string _exchange;
        private readonly ConnectionFactory _connectionFactory;
        private readonly IConnection _connection;
        private bool disposedValue;

        public string Name => "Rabbit MQ Publisher";

        public RabbitMqMessagePublisherService(IOptions<InformaticsGatewayConfiguration> options,
                                               ILogger<RabbitMqMessagePublisherService> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = options.Value.Messaging;

            ValidateConfiguration(_configuration);
            _endpoint = _configuration.PublisherSettings[ConfigurationKeys.EndPoint];
            _username = _configuration.PublisherSettings[ConfigurationKeys.Username];
            _password = _configuration.PublisherSettings[ConfigurationKeys.Password];
            _virtualHost = _configuration.SubscriberSettings[ConfigurationKeys.VirtualHost];
            _exchange = _configuration.SubscriberSettings[ConfigurationKeys.Exchange];

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _endpoint,
                UserName = _username,
                Password = _password,
                VirtualHost = _virtualHost
            };
            _connection = _connectionFactory.CreateConnection();
        }

        private void ValidateConfiguration(MessageBrokerConfiguration configuration)
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

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "MessageId", message.MessageId } });

            _logger.Log(LogLevel.Information, $"Publishing message to {_endpoint}/{_virtualHost}. Exchange={_exchange}, Routing Key={topic}");

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
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_connection != null)
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
