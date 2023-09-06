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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class PayloadRepository : IPayloadRepository, IDisposable
    {
        private readonly ILogger<PayloadRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<Payload> _collection;
        private bool _disposedValue;

        public PayloadRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PayloadRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options,
            IOptions<DatabaseOptions> mongoDbOptions)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));
            Guard.Against.Null(mongoDbOptions, nameof(mongoDbOptions));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));

            var mongoDbClient = _scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var mongoDatabase = mongoDbClient.GetDatabase(mongoDbOptions.Value.DatabaseName);
            _collection = mongoDatabase.GetCollection<Payload>(nameof(Payload));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var options = new CreateIndexOptions { Unique = true };

            var indexDefinitionState = Builders<Payload>.IndexKeys
                .Ascending(_ => _.State);
            _collection.Indexes.CreateOne(new CreateIndexModel<Payload>(indexDefinitionState));
        }

        public Task<Payload> AddAsync(Payload item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
                return item;
            });
        }

        public Task<Payload> RemoveAsync(Payload entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.DeleteOneAsync(Builders<Payload>.Filter.Where(p => p.PayloadId == entity.PayloadId), cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result.DeletedCount == 0)
                {
                    throw new DatabaseException("Failed to delete entity");
                }
                return entity;
            });
        }

        public Task<List<Payload>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(() =>
            {
                return _collection.Find(Builders<Payload>.Filter.Empty).ToListAsync(cancellationToken);
            });
        }

        public Task<Payload> UpdateAsync(Payload entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection
                    .ReplaceOneAsync(p => p.PayloadId == entity.PayloadId, entity, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (result.ModifiedCount == 0)
                {
                    throw new DatabaseException("Failed to update entity");
                }
                return entity;
            });
        }

        public Task<int> RemovePendingPayloadsAsync(CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                var results = await _collection.DeleteManyAsync(Builders<Payload>.Filter.Where(p => p.State == Payload.PayloadState.Created && p.MachineName == Environment.MachineName), cancellationToken).ConfigureAwait(false);
                return Convert.ToInt32(results.DeletedCount);
            });
        }

        public Task<List<Payload>> GetPayloadsInStateAsync(CancellationToken cancellationToken = default, params Payload.PayloadState[] states)
        {
            return _retryPolicy.ExecuteAsync(() =>
            {
                return _collection.Find(Builders<Payload>.Filter.Where(p => states.Contains(p.State))).ToListAsync(cancellationToken);
            });
        }

        public Task<IList<Payload>> GetAllAsync(int? skip, int? limit, string patientId, string patientName)
        {
            var builder = Builders<Payload>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(patientId) && !string.IsNullOrEmpty(patientName))
                filter &= builder.Exists(p => p.PatientDetails);
            if (!string.IsNullOrEmpty(patientId))
                filter &= builder.Regex(p => p.PatientDetails!.PatientId, new BsonRegularExpression($"/{patientId}/i"));
            if (!string.IsNullOrEmpty(patientName))
                filter &= builder.Regex(p => p.PatientDetails!.PatientName, new BsonRegularExpression($"/{patientName}/i"));

            return GetAllAsync(_collection,
                filter,
                Builders<Payload>.Sort.Descending(x => x.DateTimeCreated),
                skip,
                limit);
        }

        private static async Task<IList<T>> GetAllAsync<T>(IMongoCollection<T> collection,
            FilterDefinition<T> filterFunction, SortDefinition<T> sortFunction, int? skip = null, int? limit = null)
        {
            return await collection
                .Find(filterFunction)
                .Skip(skip)
                .Limit(limit)
                .Sort(sortFunction)
                .ToListAsync().ConfigureAwait(false);
        }

        public Task<long> CountAsync()
            => _collection.CountDocumentsAsync(Builders<Payload>.Filter.Empty);

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
