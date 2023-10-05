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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Logging;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    public abstract class InferenceRequestRepositoryBase : IInferenceRequestRepository
    {
        private readonly ILogger _logger;
        private readonly IOptions<DatabaseOptions> _options;

        protected InferenceRequestRepositoryBase(
            ILogger logger,
            IOptions<DatabaseOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public virtual async Task<bool> ExistsAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            return await GetInferenceRequestAsync(transactionId, cancellationToken).ConfigureAwait(false) is not null;
        }

        public virtual async Task<InferenceStatusResponse?> GetStatusAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            var response = new InferenceStatusResponse();
            var item = await GetInferenceRequestAsync(transactionId, cancellationToken).ConfigureAwait(false);
            if (item is null)
            {
                return null;
            }

            response.TransactionId = item.TransactionId;

            return await Task.FromResult(response).ConfigureAwait(false);
        }

        public async Task UpdateAsync(InferenceRequest inferenceRequest, InferenceRequestStatus status, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });

            if (status == InferenceRequestStatus.Success)
            {
                inferenceRequest.State = InferenceRequestState.Completed;
                inferenceRequest.Status = InferenceRequestStatus.Success;
            }
            else
            {
                if (++inferenceRequest.TryCount > _options.Value.Retries.DelaysMilliseconds.Length)
                {
                    _logger.InferenceRequestUpdateExceededMaximumRetries();
                    inferenceRequest.State = InferenceRequestState.Completed;
                    inferenceRequest.Status = InferenceRequestStatus.Fail;
                }
                else
                {
                    _logger.InferenceRequestUpdateRetryLater();
                    inferenceRequest.State = InferenceRequestState.Queued;
                }
            }

            await SaveAsync(inferenceRequest, cancellationToken).ConfigureAwait(false);
        }

        public abstract Task AddAsync(InferenceRequest inferenceRequest, CancellationToken cancellationToken = default);

        public abstract Task<InferenceRequest> TakeAsync(CancellationToken cancellationToken = default);

        public abstract Task<InferenceRequest?> GetInferenceRequestAsync(string transactionId, CancellationToken cancellationToken = default);

        public abstract Task<InferenceRequest?> GetInferenceRequestAsync(Guid inferenceRequestId, CancellationToken cancellationToken = default);

        protected abstract Task SaveAsync(InferenceRequest inferenceRequest, CancellationToken cancellationToken = default);
    }
}
