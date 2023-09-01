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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.EntityFramework
{
    public class RemoteAppExecutionRepository : IRemoteAppExecutionRepository, IDisposable
    {
        private readonly ILogger<RemoteAppExecutionRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly RemoteAppExecutionDbContext _dbContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<RemoteAppExecution> _dataset;
        private bool _disposedValue;

        // Note. this implementation (unlike the Mongo one) Does not delete the entries
        // so a cleanup routine will have to be implemented to peridoically remove old entries !

        public RemoteAppExecutionRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RemoteAppExecutionRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<RemoteAppExecutionDbContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _dbContext.Set<RemoteAppExecution>();
        }

        public async Task<bool> AddAsync(RemoteAppExecution item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await _dataset.AddAsync(item, cancellationToken).ConfigureAwait(false);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        public async Task<RemoteAppExecution> RemoveAsync(RemoteAppExecution remoteAppExecution, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(remoteAppExecution, nameof(remoteAppExecution));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Remove(remoteAppExecution);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            }).ConfigureAwait(false);
        }

        public async Task<RemoteAppExecution?> GetAsync(string sopInstanceUid, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.SingleOrDefaultAsync(p =>
                    p.SopInstanceUid.Equals(sopInstanceUid)).ConfigureAwait(false);
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
                var result = await _dataset.SingleOrDefaultAsync(p =>
                    p.WorkflowInstanceId.Equals(workflowInstanceId) &&
                    p.ExportTaskId.Equals(exportTaskId) &&
                    p.StudyInstanceUid.Equals(studyInstanceUid) &&
                    p.SeriesInstanceUid.Equals(seriesInstanceUid)).ConfigureAwait(false);

                result ??= await _dataset.SingleOrDefaultAsync(p =>
                        p.WorkflowInstanceId.Equals(workflowInstanceId) &&
                        p.ExportTaskId.Equals(exportTaskId) &&
                        p.StudyInstanceUid.Equals(studyInstanceUid)).ConfigureAwait(false);

                return result;
            }).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
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
