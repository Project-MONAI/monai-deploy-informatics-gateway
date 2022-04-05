// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Globalization;
using System.Text;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using Monai.Deploy.MessageBroker.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public sealed class RabbitMqHooks
    {
        internal static readonly string ScenarioContextKey = "MESSAAGES";
        private readonly string _queueNameWorkflowQueue = "workflow-queue";
        private readonly string _queueNameExportQueue = "export-queue";
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly ScenarioContext _scenarioContext;
        private readonly ConnectionFactory _connectionFactory;
        private readonly MessageBrokerConfigurationKeys _configurationKeys;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private string _consumerTag;
        public CountdownEvent MessageWaitHandle { get; private set; }

        public RabbitMqHooks(ISpecFlowOutputHelper outputHelper, Configurations configuration, ScenarioContext scenarioContext)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            _connectionFactory = new ConnectionFactory()
            {
                HostName = _configuration.MessageBrokerOptions.Endpoint,
                UserName = _configuration.MessageBrokerOptions.Username,
                Password = _configuration.MessageBrokerOptions.Password,
                VirtualHost = _configuration.MessageBrokerOptions.VirtualHost
            };

            _configurationKeys = new MessageBrokerConfigurationKeys();
            outputHelper.WriteLine($"Message broker connecting to {_configuration.MessageBrokerOptions.Endpoint}/{_configuration.MessageBrokerOptions.VirtualHost}");
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_configuration.MessageBrokerOptions.Exchange, ExchangeType.Topic);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        [BeforeScenario("@messaging_export_complete")]
        public void BeforeMessagingExportComplete()
        {
            BeforeMessagingSubscribeTo(_queueNameExportQueue, _configurationKeys.ExportComplete);
        }

        [BeforeScenario("@messaging_workflow_request")]
        public void BeforeMessagingWorkflowRequest()
        {
            BeforeMessagingSubscribeTo(_queueNameWorkflowQueue, _configurationKeys.WorkflowRequest);
        }

        private void BeforeMessagingSubscribeTo(string queue, string routingKey)
        {
            if (_scenarioContext.ContainsKey(ScenarioContextKey) && _scenarioContext[ScenarioContextKey] is IList<Message> messages && messages.Count > 0)
            {
                _outputHelper.WriteLine($"Existing message queue wasn't empty and contains {messages.Count} messages but will be cleared.");
            }
            _scenarioContext.Add(ScenarioContextKey, new List<Message>());
            _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue, _configuration.MessageBrokerOptions.Exchange, routingKey);
            var messagesPurged = _channel.QueuePurge(queue);
            _outputHelper.WriteLine($"{messagesPurged} messages purged from the queue {queue}.");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, eventArgs) =>
            {
                _outputHelper.WriteLine($"Message received from queue {queue} for {routingKey}.");

                var messsage = new Message(
                     body: eventArgs.Body.ToArray(),
                     messageDescription: routingKey,
                     messageId: eventArgs.BasicProperties.MessageId,
                     applicationId: eventArgs.BasicProperties.AppId,
                     contentType: eventArgs.BasicProperties.ContentType,
                     correlationId: eventArgs.BasicProperties.CorrelationId,
                     creationDateTime: DateTime.Parse(Encoding.UTF8.GetString((byte[])eventArgs.BasicProperties.Headers["CreationDateTime"]), CultureInfo.InvariantCulture),
                     deliveryTag: eventArgs.DeliveryTag.ToString(CultureInfo.InvariantCulture));

                (_scenarioContext[ScenarioContextKey] as IList<Message>)?.Add(messsage);
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                _outputHelper.WriteLine($"{DateTime.UtcNow} - {routingKey} message received with correlation ID={messsage.CorrelationId}, delivery tag={messsage.DeliveryTag}");
                MessageWaitHandle.Signal();
            };
            _channel.BasicQos(0, 0, false);
            _consumerTag = _channel.BasicConsume(queue, false, consumer);
            _outputHelper.WriteLine($"Listening for messages from {_configuration.MessageBrokerOptions.Endpoint}/{_configuration.MessageBrokerOptions.VirtualHost}. Exchange={_configuration.MessageBrokerOptions.Exchange}, Queue={queue}, Routing Key={routingKey}");
        }

        internal void Publish(string routingKey, Message message)
        {
            var propertiesDictionary = new Dictionary<string, object>
            {
                { "CreationDateTime", message.CreationDateTime.ToString("o") }
            };

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = message.ContentType;
            properties.MessageId = message.MessageId;
            properties.AppId = message.ApplicationId;
            properties.CorrelationId = message.CorrelationId;
            properties.DeliveryMode = 2;

            properties.Headers = propertiesDictionary;
            _channel.BasicPublish(exchange: _configuration.MessageBrokerOptions.Exchange,
                                 routingKey: routingKey,
                                 basicProperties: properties,
                                 body: message.Body);
        }

        [AfterScenario("@messaging")]
        public void AfterScenario()
        {
            _scenarioContext.Remove(ScenarioContextKey);
            _channel.BasicCancel(_consumerTag);
        }

        public void SetupMessageHandle(int count)
        {
            MessageWaitHandle = new CountdownEvent(count);
        }
    }
}
