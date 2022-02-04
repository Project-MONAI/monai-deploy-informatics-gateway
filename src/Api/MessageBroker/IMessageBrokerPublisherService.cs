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

using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Api.MessageBroker
{
    public interface IMessageBrokerPublisherService
    {
        /// <summary>
        /// Gets or sets the name of the storage service.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Publishes a message to the service.
        /// </summary>
        /// <param name="topic">Topic where the message is published to</param>
        /// <param name="message">Message to be published</param>
        /// <returns></returns>
        Task Publish(string topic, Message message);
    }
}
