/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License", CancellationToken cancellationToken = default);
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

using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    /// <summary>
    /// Interface for accessing storage metadata objects.
    /// </summary>
    public interface IStorageMetadataRepository
    {
        /// <summary>
        /// Adds new storage metadata object to the repository.
        /// </summary>
        /// <param name="metadata">The storage metadata object to be added.</param>
        Task AddAsync(FileStorageMetadata metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an storage metadata object's status.
        /// </summary>
        /// <param name="metadata">The storage metadata object to be updated.</param>
        Task UpdateAsync(FileStorageMetadata metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds or updates an storage metadata object's status.
        /// </summary>
        /// <param name="metadata">The storage metadata object to be added/updated.</param>
        Task AddOrUpdateAsync(FileStorageMetadata metadata, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all storage metadata objects associated with the correlation ID.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        Task<IList<FileStorageMetadata>> GetFileStorageMetdadataAsync(string correlationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the specified storage metadata object.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="identity">The unique identity representing the object.</param>
        Task<FileStorageMetadata?> GetFileStorageMetdadataAsync(string correlationId, string identity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the specified storage metadata object.
        /// </summary>
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="identity">The unique identity representing the object.</param>
        Task<bool> DeleteAsync(string correlationId, string identity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all pending storage metadata objects.
        /// </summary>
        Task DeletePendingUploadsAsync(CancellationToken cancellationToken = default);
    }
}
