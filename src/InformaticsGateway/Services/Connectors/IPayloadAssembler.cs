// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
        Task Queue(string bucket, FileStorageInfo file);

        /// <summary>
        /// Queue a new file for the spcified payload bucket.
        /// </summary>
        /// <param name="bucket">The bucket group the file belongs to.</param>
        /// <param name="file">Path to the file to be added to the payload bucket.</param>
        /// <param name="timeout">Number of seconds to wait for additional files.</param>
        Task Queue(string bucket, FileStorageInfo file, uint timeout);

        /// <summary>
        /// Dequeue a payload from the queue for the message broker to notify subscribers.
        /// The default implementation blocks the call until a file is available from the queue.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        Payload Dequeue(CancellationToken cancellationToken);
    }
}
