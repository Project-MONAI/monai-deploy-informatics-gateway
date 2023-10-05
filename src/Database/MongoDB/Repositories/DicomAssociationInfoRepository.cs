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
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class DicomAssociationInfoRepository : MongoDBRepositoryBase, IDicomAssociationInfoRepository, IDisposable
    {
        private readonly ILogger<DicomAssociationInfoRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<DicomAssociationInfo> _collection;
        private bool _disposedValue;

        public DicomAssociationInfoRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DicomAssociationInfoRepository> logger,
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
            _collection = mongoDatabase.GetCollection<DicomAssociationInfo>(nameof(DicomAssociationInfo));
        }

        public async Task<DicomAssociationInfo> AddAsync(DicomAssociationInfo item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
                return item;
            }).ConfigureAwait(false);
        }

        public async Task<List<DicomAssociationInfo>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _collection.Find(Builders<DicomAssociationInfo>.Filter.Empty).ToListAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public Task<IList<DicomAssociationInfo>> GetAllAsync(int skip,
            int? limit,
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken)
        {
            var builder = Builders<DicomAssociationInfo>.Filter;
            var filter = builder.Empty;
            filter &= builder.Where(t => t.DateTimeDisconnected >= startTime.ToUniversalTime());
            filter &= builder.Where(t => t.DateTimeDisconnected <= endTime.ToUniversalTime());

            return GetAllAsync(_collection,
                filter,
                Builders<DicomAssociationInfo>.Sort.Descending(x => x.DateTimeDisconnected),
                skip,
                limit);
        }

        public Task<long> CountAsync()
        {
            return _collection.CountDocumentsAsync(Builders<DicomAssociationInfo>.Filter.Empty);
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
