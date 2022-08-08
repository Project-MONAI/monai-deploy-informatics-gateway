/*
 * Copyright 2021-2022 MONAI Consortium
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

using System.Threading;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Connectors
{
    /// <summary>
    /// Interface of the Instance Stored Notification Service
    /// </summary>
    internal interface IPayloadAssembler
    {
        /// <summary>
        /// Queue a new file for the spcified payload bucket.
        /// </summary>
        /// <param name="bucket">The bucket group the file belongs to.</param>
        /// <param name="file">Path to the file to be added to the payload bucket.</param>
        Task Queue(string bucket, FileStorageMetadata file);

        /// <summary>
        /// Queue a new file for the spcified payload bucket.
        /// </summary>
        /// <param name="bucket">The bucket group the file belongs to.</param>
        /// <param name="file">Path to the file to be added to the payload bucket.</param>
        /// <param name="timeout">Number of seconds to wait for additional files.</param>
        Task Queue(string bucket, FileStorageMetadata file, uint timeout);

        /// <summary>
        /// Dequeue a payload from the queue for the message broker to notify subscribers.
        /// The default implementation blocks the call until a file is available from the queue.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        Payload Dequeue(CancellationToken cancellationToken);
    }
}
