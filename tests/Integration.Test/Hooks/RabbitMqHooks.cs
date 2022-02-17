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

using System.Text;
using Monai.Deploy.InformaticsGateway.Api.MessageBroker;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Integration.Test.Drivers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public sealed class RabbitMqHooks
    {
        internal static readonly string ScenarioContextKey = "MESSAAGES";
        private readonly string QueueName = "TestQueue";
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly Configurations _configuration;
        private readonly ScenarioContext _scenarioContext;
        private ConnectionFactory _connectionFactory;
        private MessageBrokerConfigurationKeys _configurationKeys;
        private IConnection _connection;
        private IModel _channel;
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

        [BeforeScenario("@messaging")]
        public void BeforeScenarioWithTag()
        {
            if (_scenarioContext.ContainsKey(ScenarioContextKey))
            {
                var messages = _scenarioContext[ScenarioContextKey] as IList<Message>;
                if (messages != null && messages.Count > 0)
                {
                    _outputHelper.WriteLine($"Existing message queue wasn't empty and contains {messages.Count} messages but will be cleared.");
                }
            }
            _scenarioContext.Add(ScenarioContextKey, new List<Message>());
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, _configuration.MessageBrokerOptions.Exchange, _configurationKeys.WorkflowRequest);
            var messagesPurged = _channel.QueuePurge(QueueName);
            _outputHelper.WriteLine($"{messagesPurged} messages purged from the queue {QueueName}.");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, eventArgs) =>
            {
                _outputHelper.WriteLine($"Message received from queue {QueueName} for {_configurationKeys.WorkflowRequest}.");

                var messsage = new Message(
                     body: eventArgs.Body.ToArray(),
                     bodyDescription: _configurationKeys.WorkflowRequest,
                     messageId: eventArgs.BasicProperties.MessageId,
                     applicationId: eventArgs.BasicProperties.AppId,
                     contentType: eventArgs.BasicProperties.ContentType,
                     correlationId: eventArgs.BasicProperties.CorrelationId,
                     creationDateTime: DateTime.Parse(Encoding.UTF8.GetString((byte[])eventArgs.BasicProperties.Headers["CreationDateTime"])),
                     deliveryTag: eventArgs.DeliveryTag.ToString());

                (_scenarioContext[ScenarioContextKey] as IList<Message>)?.Add(messsage);
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                _outputHelper.WriteLine($"{_configurationKeys.WorkflowRequest} message received with correlation ID={messsage.CorrelationId}, delivery tag={messsage.DeliveryTag}");
                MessageWaitHandle.Signal();
            };
            _channel.BasicQos(0, 0, false);
            _consumerTag = _channel.BasicConsume(QueueName, false, consumer);
            _outputHelper.WriteLine($"Listening for messages from {_configuration.MessageBrokerOptions.Endpoint}/{_configuration.MessageBrokerOptions.VirtualHost}. Exchange={_configuration.MessageBrokerOptions.Exchange}, Queue={QueueName}, Routing Key={_configurationKeys.WorkflowRequest}");
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
