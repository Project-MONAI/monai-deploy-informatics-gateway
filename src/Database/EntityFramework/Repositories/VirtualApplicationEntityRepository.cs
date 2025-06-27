﻿/*
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

using System.Linq.Expressions;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class VirtualApplicationEntityRepository : IVirtualApplicationEntityRepository, IDisposable
    {
        private readonly ILogger<VirtualApplicationEntityRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<VirtualApplicationEntity> _dataset;
        private bool _disposedValue;

        public VirtualApplicationEntityRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<VirtualApplicationEntityRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<VirtualApplicationEntity>();
        }

        public async Task<VirtualApplicationEntity> AddAsync(VirtualApplicationEntity item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dataset.AddAsync(item, cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            }).ConfigureAwait(false);
        }

        public async Task<bool> ContainsAsync(Expression<Func<VirtualApplicationEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var func = predicate.Compile();
                return await Task.FromResult(_dataset.Any(func)).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<VirtualApplicationEntity?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.FirstOrDefaultAsync(p => p.Name.Equals(name), cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<VirtualApplicationEntity?> FindByAeTitleAsync(string aeTitle, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(aeTitle, nameof(aeTitle));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.FirstOrDefaultAsync(p => p.VirtualAeTitle.Equals(aeTitle), cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<VirtualApplicationEntity> RemoveAsync(VirtualApplicationEntity entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Remove(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            }).ConfigureAwait(false);
        }

        public async Task<List<VirtualApplicationEntity>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.ToListAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<VirtualApplicationEntity> UpdateAsync(VirtualApplicationEntity entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Update(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
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
