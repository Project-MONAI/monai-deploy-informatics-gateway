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
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.MongoDb
{
    public class RemoteAppExecutionRepository : IRemoteAppExecutionRepository, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServiceScope _scope;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMongoCollection<RemoteAppExecution> _collection;
        private bool _disposedValue;

        public RemoteAppExecutionRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILoggerFactory loggerFactory,
            IOptions<DatabaseOptions> options
            )
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));

            _logger = loggerFactory.CreateLogger<RemoteAppExecutionRepository>();

            _scope = serviceScopeFactory.CreateScope();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));

            var mongoDbClient = _scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var mongoDatabase = mongoDbClient.GetDatabase(options.Value.DatabaseName);
            _collection = mongoDatabase.GetCollection<RemoteAppExecution>(nameof(RemoteAppExecution));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var options = new CreateIndexOptions { Unique = true };
            var indexDefinitionState = Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.SopInstanceUid);
            _collection.Indexes.CreateOne(new CreateIndexModel<RemoteAppExecution>(indexDefinitionState, options));

            var indexDefinitionSeriesLevel = Builders<RemoteAppExecution>.IndexKeys.Combine(
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.WorkflowInstanceId),
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.ExportTaskId),
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.StudyInstanceUid),
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.SeriesInstanceUid));
            _collection.Indexes.CreateOne(new CreateIndexModel<RemoteAppExecution>(indexDefinitionSeriesLevel, options));

            var indexDefinitionStudyLevel = Builders<RemoteAppExecution>.IndexKeys.Combine(
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.WorkflowInstanceId),
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.ExportTaskId),
                Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.StudyInstanceUid));
            _collection.Indexes.CreateOne(new CreateIndexModel<RemoteAppExecution>(indexDefinitionStudyLevel, options));

            options = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(7), Name = "RequestTime" };
            indexDefinitionState = Builders<RemoteAppExecution>.IndexKeys.Ascending(_ => _.RequestTime);
            _collection.Indexes.CreateOne(new CreateIndexModel<RemoteAppExecution>(indexDefinitionState, options));
        }

        public async Task<bool> AddAsync(RemoteAppExecution item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await _collection.InsertOneAsync(item, cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        public async Task<RemoteAppExecution> RemoveAsync(RemoteAppExecution remoteAppExecution, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(remoteAppExecution, nameof(remoteAppExecution));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.DeleteOneAsync(Builders<RemoteAppExecution>.Filter.Where(p => p.Id == remoteAppExecution.Id), cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result.DeletedCount == 0)
                {
                    throw new DatabaseException("Failed to delete entity");
                }
                return remoteAppExecution;
            }).ConfigureAwait(false);
        }

        public async Task<RemoteAppExecution?> GetAsync(string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.Find(p =>
                    p.SopInstanceUid.Equals(sopInstanceUid, StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync().ConfigureAwait(false);

                return result;
            }).ConfigureAwait(false);
        }

        public async Task<RemoteAppExecution?> GetAsync(string workflowInstanceId, string exportTaskId, string studyInstanceUid, string seriesInstanceUid, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(workflowInstanceId, nameof(workflowInstanceId));
            Guard.Against.NullOrWhiteSpace(exportTaskId, nameof(exportTaskId));
            Guard.Against.NullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
            Guard.Against.NullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _collection.Find(p =>
                    p.WorkflowInstanceId.Equals(workflowInstanceId, StringComparison.OrdinalIgnoreCase) &&
                    p.ExportTaskId.Equals(exportTaskId, StringComparison.OrdinalIgnoreCase) &&
                    p.StudyInstanceUid.Equals(studyInstanceUid, StringComparison.OrdinalIgnoreCase) &&
                    p.SeriesInstanceUid.Equals(seriesInstanceUid, StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync().ConfigureAwait(false);

                result ??= await _collection.Find(p =>
                    p.WorkflowInstanceId.Equals(workflowInstanceId, StringComparison.OrdinalIgnoreCase) &&
                    p.ExportTaskId.Equals(exportTaskId, StringComparison.OrdinalIgnoreCase) &&
                    p.StudyInstanceUid.Equals(studyInstanceUid, StringComparison.OrdinalIgnoreCase)).FirstOrDefaultAsync().ConfigureAwait(false);

                return result;
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
