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

using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Database.Api.Repositories
{
    /// <summary>
    /// Interface for access stored inference requests.
    /// </summary>
    public interface IInferenceRequestRepository
    {
        /// <summary>
        /// Adds new inference request to the repository.
        /// </summary>
        /// <param name="inferenceRequest">The inference request to be added.</param>
        Task AddAsync(InferenceRequest inferenceRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an inference request's status.
        /// The default implementation drops the request after 3 retries if status is
        /// <see cref="InferenceRequestStatus.Fail" />.
        /// </summary>
        /// <param name="inferenceRequest">The inference request to be updated.</param>
        /// <param name="status">Current status of the inference request.</param>
        Task UpdateAsync(InferenceRequest inferenceRequest, InferenceRequestStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>Take</c> returns the next pending inference request for data retrieval.
        /// The default implementation blocks the call until a pending inference request is available for process.
        /// </summary>
        /// <param name="cancellationToken">cancellation token used to cancel the action.</param>
        /// <returns><see cref="InferenceRequest"/></returns>
        Task<InferenceRequest> TakeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>Get</c> returns the specified inference request.
        /// </summary>
        /// <param name="transactionId">The transactionId of the request.</param>
        Task<InferenceRequest?> GetInferenceRequestAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>Get</c> returns the specified inference request.
        /// </summary>
        /// <param name="inferenceRequestId">The internal ID of the request.</param>
        Task<InferenceRequest?> GetInferenceRequestAsync(Guid inferenceRequestId, CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>Exists</c> checks whether if an existing request with the same transaction ID exists.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>GetStatus</c> returns the status of the specified inference request.
        /// </summary>
        /// <param name="transactionId">The transactionId from the original request.</param>
        Task<InferenceStatusResponse?> GetStatusAsync(string transactionId, CancellationToken cancellationToken = default);
    }
}
