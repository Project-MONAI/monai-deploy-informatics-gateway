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
using System.Threading;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
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