/*
 * Copyright 2023 MONAI Consortium
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class Hl7ApplicationConfigRepository : IHl7ApplicationConfigRepository, IDisposable
    {
        private readonly ILogger<Hl7ApplicationConfigRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<Hl7ApplicationConfigEntity> _collection;
        private bool _disposedValue;

        public Hl7ApplicationConfigRepository(IServiceScopeFactory serviceScopeFactory,
            ILogger<Hl7ApplicationConfigRepository> logger,
            IOptions<DatabaseOptions> options)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));

            var mongoDbClient = _scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var mongoDatabase = mongoDbClient.GetDatabase(options.Value.DatabaseName);
            _collection = mongoDatabase.GetCollection<Hl7ApplicationConfigEntity>(nameof(Hl7ApplicationConfigEntity));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var options = new CreateIndexOptions { Unique = true };

            var indexDefinition = Builders<Hl7ApplicationConfigEntity>.IndexKeys
                .Ascending(_ => _.DateTimeCreated);
            _collection.Indexes.CreateOne(new CreateIndexModel<Hl7ApplicationConfigEntity>(indexDefinition, options));
        }

        public Task<List<Hl7ApplicationConfigEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _retryPolicy.ExecuteAsync(() =>
                _collection.Find(Builders<Hl7ApplicationConfigEntity>.Filter.Empty).ToListAsync(cancellationToken));

        public Task<Hl7ApplicationConfigEntity?> GetByIdAsync(string id) =>
            _retryPolicy.ExecuteAsync(() => _collection
                .Find(x => x.Id.Equals(id))
                .FirstOrDefaultAsync())!;

        public Task<Hl7ApplicationConfigEntity> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                var entity = await GetByIdAsync(id).ConfigureAwait(false);
                if (entity is null)
                {
                    throw new DatabaseException("Failed to delete entity.");
                }

                var result = await _collection
                    .DeleteOneAsync(Builders<Hl7ApplicationConfigEntity>.Filter.Where(p => p.Id.Equals(id)),
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (result.DeletedCount == 0)
                {
                    throw new DatabaseException("Failed to delete entity");
                }

                return entity;
            });
        }

        public Task<Hl7ApplicationConfigEntity> CreateAsync(Hl7ApplicationConfigEntity configEntity,
            CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(configEntity, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return configEntity;
            });
        }

        public Task<Hl7ApplicationConfigEntity?> UpdateAsync(Hl7ApplicationConfigEntity configEntity,
            CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection
                    .ReplaceOneAsync(Builders<Hl7ApplicationConfigEntity>.Filter.Where(p => p.Id.Equals(configEntity.Id)),
                        configEntity, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result.ModifiedCount == 0)
                {
                    throw new DatabaseException("Failed to update entity");
                }

                return configEntity;
            })!;
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
