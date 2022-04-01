// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Monai.Deploy.MessageBroker.Common;
using Monai.Deploy.MessageBroker.Messages;

namespace Monai.Deploy.MessageBroker
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
