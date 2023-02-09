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
using System.Diagnostics;
using Monai.Deploy.Messaging.Messages;
using Monai.Deploy.Messaging.RabbitMQ;
using TechTalk.SpecFlow.Infrastructure;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    internal class RabbitMqConsumer : IDisposable
    {
        private readonly RabbitMQMessageSubscriberService _subscriberService;
        private readonly string _queueName;
        private readonly ISpecFlowOutputHelper _outputHelper;
        private readonly ConcurrentBag<Message> _messages;
        private bool _disposedValue;

        public IReadOnlyList<Message> Messages
        { get { return _messages.ToList(); } }

        public RabbitMqConsumer(RabbitMQMessageSubscriberService subscriberService, string queueName, ISpecFlowOutputHelper outputHelper)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException($"'{nameof(queueName)}' cannot be null or whitespace.", nameof(queueName));
            }
            _subscriberService = subscriberService ?? throw new ArgumentNullException(nameof(subscriberService));
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
                });
        }

        public void ClearMessages()
        {
            _outputHelper.WriteLine($"Clearing messages received from RabbitMQ");
            _messages.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscriberService.Dispose();
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

        internal async Task<bool> WaitforAsync(int messageCount, TimeSpan messageWaitTimeSpan)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (messageCount > _messages.Count && stopwatch.Elapsed < messageWaitTimeSpan)
            {
                await Task.Delay(100);
            }

            return messageCount >= _messages.Count;
        }
    }
}
