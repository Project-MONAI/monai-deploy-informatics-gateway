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
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Polly.Retry;
using Microsoft.Extensions.Logging;
using Polly;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class ExternalAppDetailsRepository : IExternalAppDetailsRepository, IDisposable
    {
        private readonly ILogger<ExternalAppDetailsRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<ExternalAppDetails> _dataset;
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
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<ExternalAppDetails>();
        }

        public async Task AddAsync(ExternalAppDetails details, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(details, nameof(details));

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _dataset.AddAsync(details, cancellationToken).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task<List<ExternalAppDetails>> GetAsync(string studyInstanceId, CancellationToken cancellationToken = default)
        {
            return await _dataset
                .Where(t => t.StudyInstanceUid == studyInstanceId).ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<ExternalAppDetails?> GetByPatientIdOutboundAsync(string patientId, CancellationToken cancellationToken)
        {
            return await _dataset
            .FirstOrDefaultAsync(t => t.PatientIdOutBound == patientId, cancellationToken)
            .ConfigureAwait(false);
        }

        public async Task<ExternalAppDetails?> GetByStudyIdOutboundAsync(string studyInstanceId, CancellationToken cancellationToken)
        {
            return await _dataset
            .FirstOrDefaultAsync(t => t.StudyInstanceUidOutBound == studyInstanceId, cancellationToken)
            .ConfigureAwait(false);
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
