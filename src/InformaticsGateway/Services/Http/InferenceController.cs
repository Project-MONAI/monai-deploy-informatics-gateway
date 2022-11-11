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

using System;
using System.Net;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("[controller]")]
    public class InferenceController : ControllerBase
    {
        private readonly IInferenceRequestRepository _inferenceRequestRepository;
        private readonly ILogger<InferenceController> _logger;

        public InferenceController(
            IInferenceRequestRepository inferenceRequestRepository,
            ILogger<InferenceController> logger)
        {
            _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("status/{transactionId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> JobStatus(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId);

            try
            {
                var status = await _inferenceRequestRepository.GetStatusAsync(transactionId, HttpContext.RequestAborted).ConfigureAwait(false);

                if (status is null)
                {
                    return Problem(title: "Inference request not found.", statusCode: (int)HttpStatusCode.NotFound, detail: "Unable to locate the specified request.");
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.ErrorRetrievingJobStatus(transactionId, ex);
                return Problem(title: "Failed to retrieve inference request status.", statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> NewInferenceRequest([FromBody] InferenceRequest request)
        {
            Guard.Against.Null(request);

            if (!request.IsValid(out var details))
            {
                return Problem(title: $"Invalid request", statusCode: (int)HttpStatusCode.UnprocessableEntity, detail: details);
            }

            using var _ = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", request.TransactionId } });
            try
            {

                if (await _inferenceRequestRepository.ExistsAsync(request.TransactionId, HttpContext.RequestAborted).ConfigureAwait(false))
                {
                    return Problem(title: "Conflict", statusCode: (int)HttpStatusCode.Conflict, detail: "An existing request with same transaction ID already exists.");
                }

                await _inferenceRequestRepository.AddAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorQueuingInferenceRequest(request.TransactionId, ex);
                return Problem(title: "Failed to save request", statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }

            return Ok(new InferenceRequestResponse
            {
                TransactionId = request.TransactionId
            });
        }
    }
}
