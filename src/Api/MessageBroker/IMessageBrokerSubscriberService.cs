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

using System;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public interface IMessageBrokerSubscriberService
    {
        /// <summary>
        /// Gets or sets the name of the storage service.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Subscribe to a message topic & queue.
        /// Either provide a topic, a queue or both.
        /// </summary>
        /// <param name="topic">Name of the topic to subscribe to</param>
        /// <param name="queue">Name of the queue to consume</param>
        /// <param name="messageReceivedCallback">Action to be performed when message is received</param>
        /// <param name="prefetchCount">Number of unacknowledged messages to receive at once.  Defaults to 0.</param>
        void Subscribe(string topic, string queue, Action<MessageReceivedEventArgs> messageReceivedCallback, ushort prefetchCount = 0);

        /// <summary>
        /// Acknowledge receiving of a message with the given token.
        /// </summary>
        /// <param name="message">Message to be acknowledged.</param>
        void Acknowledge(MessageBase message);

        /// <summary>
        /// Rejects a messags.
        /// </summary>
        /// <param name="message">Message to be rejected.</param>
        void Reject(MessageBase message);
    }
}