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

using System.Threading;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    /// <summary>
    /// Interface of Object Upload Queue for uploading received/retrieved data.
    /// </summary>
    internal interface IObjectUploadQueue
    {
        /// <summary>
        /// Queue a new file to be uploaded to storage service.
        /// </summary>
        /// <param name="file">File to be removed.</param>
        void Queue(FileStorageMetadata file);

        /// <summary>
        /// Dequeue a file from the queue for upload.
        /// The default implementation blocks the call until a file is available from the queue.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        Task<FileStorageMetadata> Dequeue(CancellationToken cancellationToken);
    }
}
