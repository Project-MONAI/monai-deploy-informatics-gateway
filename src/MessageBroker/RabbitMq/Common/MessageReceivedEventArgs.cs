// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Monai.Deploy.MessageBroker.Messages;

namespace Monai.Deploy.MessageBroker.Common
{
    /// <summary>
    /// Provides data for the subscribed event from a message broker.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public Message Message { get; }
        public CancellationToken CancellationToken { get; }

        public MessageReceivedEventArgs(Message message, CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;
        }
    }
}
