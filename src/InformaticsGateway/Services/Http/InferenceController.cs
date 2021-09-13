// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using System;
using System.IO.Abstractions;
using System.Net;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<ActionResult> JobStatus(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            try
            {
                var status = await _inferenceRequestRepository.GetStatus(transactionId);

                if (status is null)
                {
                    return Problem(title: "Inference request not found.", statusCode: (int)HttpStatusCode.NotFound, detail: "Unable to locate the specified request.");
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, $"Failed to retrieve status for TransactionId/JobId={transactionId}");
                return Problem(title: "Failed to retrieve inference request status.", statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> NewInferenceRequest([FromBody] InferenceRequest request)
        {
            Guard.Against.Null(request, nameof(request));

            if (!request.IsValid(out string details))
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
                    out string storagePath))
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
                _logger.Log(LogLevel.Error, ex, $"Failed to configure storage location for request: TransactionId={request.TransactionId}");
                return Problem(title: ex.Message, statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }

            try
            {
                await _inferenceRequestRepository.Add(request);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, $"Unable to queue the request: TransactionId={request.TransactionId}");
                return Problem(title: "Failed to save request", statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }

            return Ok(new InferenceRequestResponse
            {
                TransactionId = request.TransactionId
            });
        }
    }
}
