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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Logging;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class PayloadRepository : IPayloadRepository, IDisposable
    {
        private readonly ILogger<PayloadRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<Payload> _dataset;
        private bool _disposedValue;

        public PayloadRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PayloadRepository> logger,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            Guard.Against.Null(serviceScopeFactory);
            Guard.Against.Null(options);

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<Payload>();
        }

        public async Task<Payload> AddAsync(Payload item, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dataset.AddAsync(item, cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            }).ConfigureAwait(false);
        }

        public async Task<bool> ContainsAsync(Expression<Func<Payload, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.AnyAsync(predicate, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<Payload> RemoveAsync(Payload entity, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Remove(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            }).ConfigureAwait(false);
        }

        public async Task<List<Payload>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.ToListAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<Payload> UpdateAsync(Payload entity, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Update(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            }).ConfigureAwait(false);
        }

        public async Task<int> RemovePendingPayloadsAsync(CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var count = 0;
                await _dataset.Where(p => p.State == Payload.PayloadState.Created).ForEachAsync(
                   p =>
                   {
                       _dataset.Remove(p);
                       count++;
                   }).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return count;
            }).ConfigureAwait(false);
        }

        public async Task<List<Payload>> GetPayloadsInStateAsync(CancellationToken cancellationToken = default, params Payload.PayloadState[] states)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.Where(p => states.Contains(p.State)).ToListAsync(cancellationToken).ConfigureAwait(false);
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
