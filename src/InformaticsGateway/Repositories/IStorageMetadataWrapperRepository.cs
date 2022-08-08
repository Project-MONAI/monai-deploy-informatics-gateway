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

using System.Collections.Generic;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    /// <summary>
    /// Interface for accessing storage metadata objects.
    /// </summary>
    public interface IStorageMetadataWrapperRepository
    {
        /// <summary>
        /// Adds new storage metadata object to the repository.
        /// </summary>
        /// <param name="metadata">The storage metadata object to be added.</param>
        Task AddAsync(FileStorageMetadata metadata);

        /// <summary>
        /// Updates an storage metadata object's status.
        /// </summary>
        /// <param name="metadata">The storage metadata object to be updated.</param>
        Task UpdateAsync(FileStorageMetadata metadata);

        /// <summary>
        /// Adds or updates an storage metadata object's status.
        /// </summary>
        /// <param name="metadata">The storage metadata object to be added/updated.</param>
        Task AddOrUpdateAsync(FileStorageMetadata metadata);

        /// <summary>
        /// Gets all storage metadata objects associated with the correlation ID.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        IList<FileStorageMetadata> GetFileStorageMetdadata(string correlationId);

        /// <summary>
        /// Gets the specified storage metadata object.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="identity">The unique identity representing the object.</param>
        FileStorageMetadata GetFileStorageMetdadata(string correlationId, string identity);

        /// <summary>
        /// Deletes the specified storage metadata object.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="identity">The unique identity representing the object.</param>
        Task<bool> DeleteAsync(string correlationId, string identity);

        /// <summary>
        /// Deletes all pending storage metadata objects.
        /// </summary>
        Task DeletePendingUploadsAsync();
    }
}
