/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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

using System.Collections.Concurrent;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Messaging.RabbitMQ;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    internal class RabbitMqConsumer
    {
        private readonly string _queueName;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly ConcurrentBag<Message> _messages;

        public IReadOnlyList<Message> Messages { get { return _messages.ToList(); } }
        public CountdownEvent MessageWaitHandle { get; private set; }

        public RabbitMqConsumer(RabbitMQMessageSubscriberService subscriberService, string queueName, ISpecFlowOutputHelper outputHelper)
        {
            if (subscriberService is null)
            {
                throw new ArgumentNullException(nameof(subscriberService));
            }

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException($"'{nameof(queueName)}' cannot be null or whitespace.", nameof(queueName));
            }

            _queueName = queueName;
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _messages = new ConcurrentBag<Message>();


            subscriberService.Subscribe(
                queueName,
                queueName,
                (eventArgs) =>
                {
                    _outputHelper.WriteLine($"Message received from queue {queueName} for {queueName}.");
                    _messages.Add(eventArgs.Message);
                    subscriberService.Acknowledge(eventArgs.Message);
                    _outputHelper.WriteLine($"{DateTime.UtcNow} - {queueName} message received with correlation ID={eventArgs.Message.CorrelationId}, delivery tag={eventArgs.Message.DeliveryTag}");
                    MessageWaitHandle?.Signal();

                });
        }

        public void SetupMessageHandle(int count)
        {
            _outputHelper.WriteLine($"Expecting {count} {_queueName} messages from RabbitMQ");
            _messages.Clear();
            MessageWaitHandle = new CountdownEvent(count);
        }
    }
}
