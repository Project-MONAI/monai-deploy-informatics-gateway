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
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class StorageMetadataWrapperRepository : StorageMetadataRepositoryBase, IDisposable
    {
        private readonly ILogger<StorageMetadataWrapperRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<StorageMetadataWrapper> _dataset;
        private bool _disposedValue;

        public StorageMetadataWrapperRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<StorageMetadataWrapperRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options) : base(logger)
        {
            Guard.Against.Null(serviceScopeFactory);
            Guard.Against.Null(options);

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>(p => p is not ArgumentException).WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<StorageMetadataWrapper>();
        }

        protected override async Task AddAsyncInternal(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _dataset.AddAsync(metadata, cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _informaticsGatewayContext.Entry(metadata).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            })
            .ConfigureAwait(false);
        }

        protected override async Task UpdateInternal(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                _dataset.Update(metadata);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _informaticsGatewayContext.Entry(metadata).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                _logger.StorageMetadataSaved();
            })
            .ConfigureAwait(false);
        }

        protected override async Task<StorageMetadataWrapper?> FindByIds(string id, string correlationId, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.FirstOrDefaultAsync(p => p.Identity == id && p.CorrelationId == correlationId, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public override async Task<IList<FileStorageMetadata>> GetFileStorageMetdadataAsync(string correlationId, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(correlationId);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset
                        .Where(p => p.CorrelationId.Equals(correlationId))
                        .Select(p => p.GetObject())
                        .ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public override async Task<FileStorageMetadata?> GetFileStorageMetdadataAsync(string correlationId, string identity, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(correlationId);
            Guard.Against.NullOrWhiteSpace(identity);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dataset
                    .FirstOrDefaultAsync(p => p.CorrelationId.Equals(correlationId) && p.Identity.Equals(identity), cancellationToken)
                    .ConfigureAwait(false);
                return result?.GetObject();
            }).ConfigureAwait(false);
        }

        protected override async Task<bool> DeleteInternalAsync(StorageMetadataWrapper metadata, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(metadata);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                _dataset.Remove(metadata);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        public override async Task DeletePendingUploadsAsync(CancellationToken cancellationToken = default)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var toBeDeleted = _dataset.Where(p => !p.IsUploaded);

                if (await toBeDeleted.AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    _dataset.RemoveRange(toBeDeleted.ToArray());
                    await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _informaticsGatewayContext.Dispose();
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
