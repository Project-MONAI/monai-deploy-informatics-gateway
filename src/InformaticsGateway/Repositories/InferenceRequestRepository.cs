// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    public class InferenceRequestRepository : IInferenceRequestRepository
    {
        private const int MaxRetryLimit = 3;

        private readonly ILogger<InferenceRequestRepository> _logger;
        private readonly IInformaticsGatewayRepository<InferenceRequest> _inferenceRequestRepository;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public InferenceRequestRepository(
            ILogger<InferenceRequestRepository> logger,
            IInformaticsGatewayRepository<InferenceRequest> inferenceRequestRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
        }

        public async Task Add(InferenceRequest inferenceRequest)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                {
                    _logger.Log(LogLevel.Error, exception, $"Error saving inference request. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                })
                .ExecuteAsync(async () =>
                {
                    await _inferenceRequestRepository.AddAsync(inferenceRequest);
                    await _inferenceRequestRepository.SaveChangesAsync();
                    _inferenceRequestRepository.Detach(inferenceRequest);
                    _logger.Log(LogLevel.Debug, $"Inference request saved.");
                })
                .ConfigureAwait(false);
        }

        public async Task Update(InferenceRequest inferenceRequest, InferenceRequestStatus status)
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
                if (++inferenceRequest.TryCount > MaxRetryLimit)
                {
                    _logger.Log(LogLevel.Information, $"Exceeded maximum retries.");
                    inferenceRequest.State = InferenceRequestState.Completed;
                    inferenceRequest.Status = InferenceRequestStatus.Fail;
                }
                else
                {
                    _logger.Log(LogLevel.Information, $"Will retry later.");
                    inferenceRequest.State = InferenceRequestState.Queued;
                }
            }

            await Save(inferenceRequest);
        }

        public async Task<InferenceRequest> Take(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var inferenceRequest = _inferenceRequestRepository.FirstOrDefault(p => p.State == InferenceRequestState.Queued);

                if (inferenceRequest is not null)
                {
                    using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });
                    inferenceRequest.State = InferenceRequestState.InProcess;
                    _logger.Log(LogLevel.Debug, $"Updating request {inferenceRequest.TransactionId} to InProgress.");
                    await Save(inferenceRequest);
                    return inferenceRequest;
                }
                await Task.Delay(250, cancellationToken);
            }

            throw new OperationCanceledException("cancellation requsted");
        }

        public InferenceRequest GetInferenceRequest(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            return _inferenceRequestRepository.FirstOrDefault(p => p.TransactionId.Equals(transactionId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<InferenceRequest> GetInferenceRequest(Guid inferenceRequestId)
        {
            Guard.Against.NullOrEmpty(inferenceRequestId, nameof(inferenceRequestId));
            return await _inferenceRequestRepository.FindAsync(inferenceRequestId);
        }

        public bool Exists(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            return GetInferenceRequest(transactionId) is not null;
        }

        public async Task<InferenceStatusResponse> GetStatus(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            var response = new InferenceStatusResponse();
            var item = GetInferenceRequest(transactionId);
            if (item is null)
            {
                return null;
            }

            response.TransactionId = item.TransactionId;

            return await Task.FromResult(response);
        }

        private async Task Save(InferenceRequest inferenceRequest)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            await Policy
                 .Handle<Exception>()
                 .WaitAndRetryAsync(
                     3,
                     retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                     (exception, timeSpan, retryCount, context) =>
                     {
                         _logger.Log(LogLevel.Error, exception, $"Error while updating inference request. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                     })
                 .ExecuteAsync(async () =>
                 {
                     _logger.Log(LogLevel.Debug, $"Updating inference request.");
                     if (inferenceRequest.State == InferenceRequestState.Completed)
                     {
                         _inferenceRequestRepository.Detach(inferenceRequest);
                     }
                     await _inferenceRequestRepository.SaveChangesAsync();
                     _logger.Log(LogLevel.Information, $"Inference request updated.");
                 })
                 .ConfigureAwait(false);
        }
    }
}
