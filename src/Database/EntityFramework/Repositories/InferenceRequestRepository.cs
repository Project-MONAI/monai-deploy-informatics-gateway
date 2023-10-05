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

using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Polly;
using Polly.Retry;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories
{
    public class InferenceRequestRepository : InferenceRequestRepositoryBase, IDisposable
    {
        private readonly ILogger<InferenceRequestRepository> _logger;
        private readonly IServiceScope _scope;
        private readonly InformaticsGatewayContext _informaticsGatewayContext;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly DbSet<InferenceRequest> _dataset;
        private bool _disposedValue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public InferenceRequestRepository(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<InferenceRequestRepository> logger,
            IOptions<DatabaseOptions> options) : base(logger, options)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scope = serviceScopeFactory.CreateScope();
            _informaticsGatewayContext = _scope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
            _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                options.Value.Retries.RetryDelays,
                (exception, timespan, count, context) => _logger.DatabaseErrorRetry(timespan, count, exception));
            _dataset = _informaticsGatewayContext.Set<InferenceRequest>();
        }

        public override async Task AddAsync(InferenceRequest inferenceRequest, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _dataset.AddAsync(inferenceRequest).ConfigureAwait(false);
                await _informaticsGatewayContext.SaveChangesAsync().ConfigureAwait(false);
                _informaticsGatewayContext.Entry(inferenceRequest).State = EntityState.Detached;
                _logger.InferenceRequestSaved();
            })
            .ConfigureAwait(false);
        }

        public override async Task<InferenceRequest> TakeAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var inferenceRequest = await _dataset.FirstOrDefaultAsync(p => p.State == InferenceRequestState.Queued, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (inferenceRequest is not null)
                    {
                        using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });
                        inferenceRequest.State = InferenceRequestState.InProcess;
                        _logger.InferenceRequestSetToInProgress(inferenceRequest.TransactionId);
                        await SaveAsync(inferenceRequest).ConfigureAwait(false);
                        return inferenceRequest;
                    }
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorQueryingForPendingInferenceRequest(ex);
                }
            }

            throw new OperationCanceledException("cancellation requested.");
        }

        public override async Task<InferenceRequest?> GetInferenceRequestAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.FirstOrDefaultAsync(p => p.TransactionId.Equals(transactionId), cancellationToken: cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public override async Task<InferenceRequest?> GetInferenceRequestAsync(Guid inferenceRequestId, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrEmpty(inferenceRequestId, nameof(inferenceRequestId));

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _dataset.FirstOrDefaultAsync(p => p.InferenceRequestId.Equals(inferenceRequestId), cancellationToken: cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        protected override async Task SaveAsync(InferenceRequest inferenceRequest, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.InferenceRequestUpdateState();
                if (inferenceRequest.State == InferenceRequestState.Completed)
                {
                    _informaticsGatewayContext.Entry(inferenceRequest).State = EntityState.Detached;
                }
                _dataset.Update(inferenceRequest);
                await _informaticsGatewayContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.InferenceRequestUpdated();
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
