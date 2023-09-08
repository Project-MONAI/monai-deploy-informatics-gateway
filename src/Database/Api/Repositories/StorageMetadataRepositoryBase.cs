/*
 * Copyright 2022 MONAI Consortium
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

using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public abstract class StorageMetadataRepositoryBase : IStorageMetadataRepository
    {
        private readonly ILogger _logger;

        protected StorageMetadataRepositoryBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddAsync(FileStorageMetadata metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "CorrelationId", metadata.CorrelationId }, { "Identity", metadata.Id } });
            var obj = new StorageMetadataWrapper(metadata);
            await AddAsyncInternal(obj, cancellationToken).ConfigureAwait(false);
            _logger.StorageMetadataSaved();
        }

        public async Task AddOrUpdateAsync(FileStorageMetadata metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            var existing = await GetFileStorageMetdadataAsync(metadata.CorrelationId, metadata.Id, cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                await UpdateAsync(metadata, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await AddAsync(metadata, cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual async Task<bool> DeleteAsync(string correlationId, string identity, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(identity, nameof(identity));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "CorrelationId", correlationId }, { "Identity", identity } });

            var toBeDeleted = await FindByIds(identity, correlationId, cancellationToken).ConfigureAwait(false);

            if (toBeDeleted is not null)
            {
                return await DeleteInternalAsync(toBeDeleted, cancellationToken).ConfigureAwait(false);
            }
            return false;
        }

        protected abstract Task<bool> DeleteInternalAsync(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default);

        public abstract Task DeletePendingUploadsAsync(CancellationToken cancellationToken = default);

        public abstract Task<IList<FileStorageMetadata>> GetFileStorageMetdadataAsync(string correlationId, CancellationToken cancellationToken = default);

        public abstract Task<FileStorageMetadata?> GetFileStorageMetdadataAsync(string correlationId, string identity, CancellationToken cancellationToken = default);

        public virtual async Task UpdateAsync(FileStorageMetadata metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "CorrelationId", metadata.CorrelationId }, { "Identity", metadata.Id } });
            var obj = await FindByIds(metadata.Id, metadata.CorrelationId).ConfigureAwait(false);

            if (obj is null)
            {
                throw new ArgumentException("Matching wrapper storage object not found");
            }

            obj.Update(metadata);
            await UpdateInternal(obj, cancellationToken).ConfigureAwait(false);
            _logger.StorageMetadataSaved();
        }

        protected abstract Task UpdateInternal(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default);

        protected abstract Task<StorageMetadataWrapper?> FindByIds(string id, string correlationId, CancellationToken cancellationToken = default);

        protected abstract Task AddAsyncInternal(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default);
    }
}
