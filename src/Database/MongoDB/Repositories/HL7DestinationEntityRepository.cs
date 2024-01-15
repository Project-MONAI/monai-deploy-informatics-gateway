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

using System.Linq.Expressions;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class HL7DestinationEntityRepository : IHL7DestinationEntityRepository, IDisposable
    {
        private readonly ILogger<HL7DestinationEntityRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<HL7DestinationEntity> _collection;
        private bool _disposedValue;

        public HL7DestinationEntityRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<HL7DestinationEntityRepository> logger,
            IOptions<DatabaseOptions> options,
            IOptions<DatabaseOptions> mongoDbOptions)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));
            Guard.Against.Null(mongoDbOptions, nameof(mongoDbOptions));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Retries.RetryDelays,
                (exception, timespan, count, context) =>
                {
                    _logger.DatabaseErrorRetry(timespan, count, exception);
                });

            var mongoDbClient = _scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var mongoDatabase = mongoDbClient.GetDatabase(mongoDbOptions.Value.DatabaseName);
            _collection = mongoDatabase.GetCollection<HL7DestinationEntity>(nameof(HL7DestinationEntity));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var options = new CreateIndexOptions { Unique = true };

            var indexDefinition = Builders<HL7DestinationEntity>.IndexKeys
                .Ascending(_ => _.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<HL7DestinationEntity>(indexDefinition, options));

            var indexDefinitionAll = Builders<HL7DestinationEntity>.IndexKeys.Combine(
                Builders<HL7DestinationEntity>.IndexKeys.Ascending(_ => _.Name),
                Builders<HL7DestinationEntity>.IndexKeys.Ascending(_ => _.AeTitle),
                Builders<HL7DestinationEntity>.IndexKeys.Ascending(_ => _.HostIp),
                Builders<HL7DestinationEntity>.IndexKeys.Ascending(_ => _.Port));
            _collection.Indexes.CreateOne(new CreateIndexModel<HL7DestinationEntity>(indexDefinitionAll, options));
        }

        public async Task<List<HL7DestinationEntity>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _collection.Find(Builders<HL7DestinationEntity>.Filter.Empty).ToListAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<HL7DestinationEntity?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _collection
                    .Find(x => x.Name == name)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<HL7DestinationEntity> AddAsync(HL7DestinationEntity item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
                return item;
            }).ConfigureAwait(false);
        }

        public async Task<HL7DestinationEntity> UpdateAsync(HL7DestinationEntity entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.ReplaceOneAsync(p => p.Id == entity.Id, entity, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result.ModifiedCount == 0)
                {
                    throw new DatabaseException("Failed to update entity");
                }
                return entity;
            }).ConfigureAwait(false);
        }

        public async Task<HL7DestinationEntity> RemoveAsync(HL7DestinationEntity entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.DeleteOneAsync(Builders<HL7DestinationEntity>.Filter.Where(p => p.Name == entity.Name), cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result.DeletedCount == 0)
                {
                    throw new DatabaseException("Failed to delete entity");
                }
                return entity;
            }).ConfigureAwait(false);
        }

        public async Task<bool> ContainsAsync(Expression<Func<HL7DestinationEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.FindAsync(predicate, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await result.AnyAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
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
