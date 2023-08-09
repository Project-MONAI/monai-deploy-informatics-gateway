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

using System.Data;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Configurations;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class StorageMetadataWrapperRepository : StorageMetadataRepositoryBase, IDisposable
    {
        private readonly ILogger<StorageMetadataWrapperRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly IMongoCollection<StorageMetadataWrapper> _collection;
        private readonly AsyncRetryPolicy _retryPolicy;
        private bool _disposedValue;

        public StorageMetadataWrapperRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<StorageMetadataWrapperRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options,
            IOptions<MongoDBOptions> mongoDbOptions) : base(logger)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));
            Guard.Against.Null(mongoDbOptions, nameof(mongoDbOptions));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _retryPolicy = Policy.Handle<Exception>(p => p is not ArgumentException).WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));

            var mongoDbClient = _scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var mongoDatabase = mongoDbClient.GetDatabase(mongoDbOptions.Value.DatabaseName);
            _collection = mongoDatabase.GetCollection<StorageMetadataWrapper>(nameof(StorageMetadataWrapper));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexDefinition = Builders<StorageMetadataWrapper>.IndexKeys.Combine(
                Builders<StorageMetadataWrapper>.IndexKeys.Ascending(_ => _.CorrelationId),
                Builders<StorageMetadataWrapper>.IndexKeys.Ascending(_ => _.Identity));

            _collection.Indexes.CreateOne(new CreateIndexModel<StorageMetadataWrapper>(indexDefinition));
        }

        protected override async Task<bool> DeleteInternalAsync(StorageMetadataWrapper toBeDeleted, CancellationToken cancellationToken)
        {
            Guard.Against.Null(toBeDeleted, nameof(toBeDeleted));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var results = await _collection.DeleteOneAsync(Builders<StorageMetadataWrapper>.Filter.Where(p => p.Identity == toBeDeleted.Identity), cancellationToken: cancellationToken).ConfigureAwait(false);
                return results.DeletedCount == 1;
            }).ConfigureAwait(false);
        }

        public override async Task DeletePendingUploadsAsync(CancellationToken cancellationToken = default)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.DeleteManyAsync(Builders<StorageMetadataWrapper>.Filter.Where(p => !p.IsUploaded), cancellationToken);
            }).ConfigureAwait(false);
        }

        public override async Task<IList<FileStorageMetadata>> GetFileStorageMetdadataAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var results = await _collection.Find(Builders<StorageMetadataWrapper>.Filter.Where(p => p.CorrelationId == correlationId))
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);
                return results.Select(p => p.GetObject()).ToList();
            }).ConfigureAwait(false);
        }

        public override async Task<FileStorageMetadata?> GetFileStorageMetdadataAsync(string correlationId, string identity, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.Find(Builders<StorageMetadataWrapper>.Filter.Where(p => p.CorrelationId == correlationId && p.Identity == identity))
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                return result?.GetObject();
            }).ConfigureAwait(false);
        }

        protected override async Task UpdateInternal(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.ReplaceOneAsync(
                            Builders<StorageMetadataWrapper>.Filter.Where(p => p.Identity == metadata.Identity),
                            metadata,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
            })
            .ConfigureAwait(false);
        }

        protected override async Task<StorageMetadataWrapper?> FindByIds(string id, string correlationId, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _collection.Find(Builders<StorageMetadataWrapper>.Filter.Where(p => p.CorrelationId == correlationId && p.Identity == id))
                        .FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        protected override async Task AddAsyncInternal(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata, nameof(metadata));

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
            })
            .ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _scope.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
