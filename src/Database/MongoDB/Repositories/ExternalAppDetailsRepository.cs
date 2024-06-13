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
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Repositories
{
    public class ExternalAppDetailsRepository : IExternalAppDetailsRepository, IDisposable
    {
        private readonly ILogger<IExternalAppDetailsRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<ExternalAppDetails> _collection;
        private bool _disposedValue;

        public ExternalAppDetailsRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ExternalAppDetailsRepository> logger,
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
            _collection = mongoDatabase.GetCollection<ExternalAppDetails>(nameof(ExternalAppDetails));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexDefinitionState = Builders<ExternalAppDetails>.IndexKeys
                .Ascending(_ => _.StudyInstanceUid);
            _collection.Indexes.CreateOne(new CreateIndexModel<ExternalAppDetails>(indexDefinitionState));

            indexDefinitionState = Builders<ExternalAppDetails>.IndexKeys
                .Ascending(_ => _.PatientIdOutBound);
            _collection.Indexes.CreateOne(new CreateIndexModel<ExternalAppDetails>(indexDefinitionState));

            indexDefinitionState = Builders<ExternalAppDetails>.IndexKeys
                .Ascending(_ => _.StudyInstanceUidOutBound);
            _collection.Indexes.CreateOne(new CreateIndexModel<ExternalAppDetails>(indexDefinitionState));

            var options = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(7), Name = "DateTimeCreated" };
            indexDefinitionState = Builders<ExternalAppDetails>.IndexKeys.Ascending(_ => _.DateTimeCreated);
            _collection.Indexes.CreateOne(new CreateIndexModel<ExternalAppDetails>(indexDefinitionState, options));
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

        public async Task AddAsync(ExternalAppDetails details, CancellationToken cancellationToken)
        {
            Guard.Against.Null(details, nameof(details));

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(details, cancellationToken: cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<List<ExternalAppDetails>> GetAsync(string studyInstanceId, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return (await _collection.FindAsync(p =>
                p.StudyInstanceUid == studyInstanceId, null, cancellationToken
                    ).ConfigureAwait(false)).ToList();
            }).ConfigureAwait(false);
        }

        public async Task<ExternalAppDetails?> GetByPatientIdOutboundAsync(string patientId, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return (await _collection.FindAsync(p =>
                p.PatientIdOutBound == patientId, null, cancellationToken
                    ).ConfigureAwait(false)).FirstOrDefault();
            }).ConfigureAwait(false);
        }

        public async Task<ExternalAppDetails?> GetByStudyIdOutboundAsync(string studyInstanceId, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return (await _collection.FindAsync(p =>
                p.StudyInstanceUidOutBound == studyInstanceId, null, cancellationToken
                    ).ConfigureAwait(false)).FirstOrDefault();
            }).ConfigureAwait(false);
        }
    }
}
