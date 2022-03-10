// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    /// <summary>
    /// Interface for access stored inference requets.
    /// </summary>
    public interface IInferenceRequestRepository
    {
        /// <summary>
        /// Adds new inference request to the repository.
        /// </summary>
        /// <param name="inferenceRequest">The inference request to be added.</param>
        Task Add(InferenceRequest inferenceRequest);

        /// <summary>
        /// Updates an inference request's status.
        /// The default implementation drops the request after 3 retries if status is
        /// <see cref="Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequestStatus.Fail" />.
        /// </summary>
        /// <param name="inferenceRequest">The inference request to be updated.</param>
        /// <param name="status">Current status of the inference request.</param>
        Task Update(InferenceRequest inferenceRequest, InferenceRequestStatus status);

        /// <summary>
        /// <c>Take</c> returns the next pending inference request for data retrieval.
        /// The default implementation blocks the call until a pending inference request is available for process.
        /// </summary>
        /// <param name="cancellationToken">cancellation token used to cancel the action.</param>
        /// <returns><see cref="Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequest"/></returns>
        Task<InferenceRequest> Take(CancellationToken cancellationToken);

        /// <summary>
        /// <c>Get</c> returns the specified inference request.
        /// </summary>
        /// <param name="transactionId">The transactionId of the request.</param>
        InferenceRequest GetInferenceRequest(string transactionId);

        /// <summary>
        /// <c>Get</c> returns the specified inference request.
        /// </summary>
        /// <param name="inferenceRequestId">The internal ID of the request.</param>
        Task<InferenceRequest> GetInferenceRequest(Guid inferenceRequestId);

        /// <summary>
        /// <c>Exists</c> checks whether if an existing request with the same transaction ID exists.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        bool Exists(string transactionId);

        /// <summary>
        /// <c>GetStatus</c> returns the status of the specified inference request.
        /// </summary>
        /// <param name="transactionId">The transactionId from the original request.</param>
        Task<InferenceStatusResponse> GetStatus(string transactionId);
    }
}
