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
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class Hl7ApplicationConfigRepository : IHl7ApplicationConfigRepository
    {
        private readonly ILogger<Hl7ApplicationConfigRepository> _logger;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<Hl7ApplicationConfigEntity> _dataset;

        public Hl7ApplicationConfigRepository(ILogger<Hl7ApplicationConfigRepository> logger,
            IOptions<DatabaseOptions> options, IServiceScopeFactory serviceScopeFactory)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            Guard.Against.Null(options, nameof(options));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var scope = serviceScopeFactory.CreateScope();

            _informaticsGatewayContext = scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<Hl7ApplicationConfigEntity>();
        }

        public Task<List<Hl7ApplicationConfigEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _retryPolicy.ExecuteAsync(() => { return _dataset.ToListAsync(cancellationToken); });

        public Task<Hl7ApplicationConfigEntity?> GetByIdAsync(string id) =>
            _retryPolicy.ExecuteAsync(() => _dataset.FirstOrDefaultAsync(x => x.Id.Equals(id)));

        public Task<Hl7ApplicationConfigEntity> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                var entity = await GetByIdAsync(id).ConfigureAwait(false);
                var result = _dataset.Remove(entity);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            });
        }

        public Task<Hl7ApplicationConfigEntity> CreateAsync(Hl7ApplicationConfigEntity configEntity,
            CancellationToken cancellationToken = default)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dataset.AddAsync(configEntity, cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return result.Entity;
            });
        }
    }
}
