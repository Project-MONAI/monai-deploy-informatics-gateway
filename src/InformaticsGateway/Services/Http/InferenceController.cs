// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO.Abstractions;
using System.Net;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("[controller]")]
    public class InferenceController : ControllerBase
    {
        private readonly IInferenceRequestRepository _inferenceRequestRepository;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly ILogger<InferenceController> _logger;
        private readonly IFileSystem _fileSystem;

        public InferenceController(
            IInferenceRequestRepository inferenceRequestRepository,
            IOptions<InformaticsGatewayConfiguration> configuration,
            ILogger<InferenceController> logger,
            IFileSystem fileSystem)
        {
            _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        [HttpGet("status/{transactionId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> JobStatus(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            try
            {
                var status = await _inferenceRequestRepository.GetStatus(transactionId).ConfigureAwait(false);

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
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> NewInferenceRequest([FromBody] InferenceRequest request)
        {
            Guard.Against.Null(request, nameof(request));

            if (!request.IsValid(out var details))
            {
                return Problem(title: $"Invalid request", statusCode: (int)HttpStatusCode.UnprocessableEntity, detail: details);
            }

            using var _ = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", request.TransactionId } });

            if (_inferenceRequestRepository.Exists(request.TransactionId))
            {
                return Problem(title: "Conflict", statusCode: (int)HttpStatusCode.Conflict, detail: "An existing request with same transaction ID already exists.");
            }

            try
            {
                if (_fileSystem.Directory.TryGenerateDirectory(_fileSystem.Path.Combine(_configuration.Value.Storage.TemporaryDataDirFullPath, request.TransactionId),
                    out var storagePath))
                {
                    request.ConfigureTemporaryStorageLocation(storagePath);
                }
                else
                {
                    throw new InferenceRequestException("Failed to generate a temporary storage location for request.");
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorConfiguringStorageLocation(request.TransactionId, ex);
                return Problem(title: ex.Message, statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }

            try
            {
                await _inferenceRequestRepository.Add(request).ConfigureAwait(false);
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
