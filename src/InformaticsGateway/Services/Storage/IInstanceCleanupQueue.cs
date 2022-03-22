// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Threading;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    /// <summary>
    /// Interface of Instance Cleanup Queue
    /// </summary>
    internal interface IInstanceCleanupQueue
    {
        /// <summary>
        /// Queue a new file to be cleaned up.
        /// </summary>
        /// <param name="file">File to be removed.</param>
        void Queue(FileStorageInfo file);

        /// <summary>
        /// Dequeue a file from the queue for cleanup.
        /// The default implementation blocks the call until a file is available from the queue.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        FileStorageInfo Dequeue(CancellationToken cancellationToken);
    }
}
