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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
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
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scope = serviceScopeFactory.CreateScope();
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Database.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<Payload>();
        }

        public Task<Payload> AddAsync(Payload item, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(item, nameof(item));

            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dataset.AddAsync(item, cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            });
        }

        public Task<Payload> RemoveAsync(Payload entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Remove(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            });
        }

        public Task<List<Payload>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(() => _dataset.ToListAsync(cancellationToken));
        }

        public Task<Payload> UpdateAsync(Payload entity, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(entity, nameof(entity));

            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = _dataset.Update(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            });
        }

        public Task<int> RemovePendingPayloadsAsync(CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                var count = 0;
                await _dataset.Where(p => p.State == Payload.PayloadState.Created && p.MachineName == Environment.MachineName).ForEachAsync(
                    p =>
                    {
                        _dataset.Remove(p);
                        count++;
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return count;
            });
        }

        public Task<List<Payload>> GetPayloadsInStateAsync(CancellationToken cancellationToken = default, params Payload.PayloadState[] states)
        {
            return _retryPolicy.ExecuteAsync(() =>
            {
                return _dataset.Where(p => states.Contains(p.State)).ToListAsync(cancellationToken);
            });
        }

        public async Task<IList<Payload>> GetAllAsync(int? skip, int? limit, string patientId, string patientName)
        {
            return await _dataset
                .Skip(skip ?? 0)
                .Where(p => p.PatientDetails != null)
                .Where(p => p.PatientDetails!.PatientId == patientId)
                .Where(p => p.PatientDetails!.PatientName == patientName)
                .Take(limit ?? 10)
                .ToListAsync().ConfigureAwait(false);
        }

        public Task<long> CountAsync() => _dataset.LongCountAsync();

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
