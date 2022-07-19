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

using System;
using System.Net;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;

namespace Monai.Deploy.InformaticsGateway.Services.Http.DicomWeb
{
    [ApiController]
    [Route("dicomweb")]
    public class StowController : ControllerBase
    {
        private readonly IStowService _stowService;
        private readonly ILogger<StowController> _logger;

        public StowController(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory is null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            }
            var scope = serviceScopeFactory.CreateScope();

            _stowService = scope.ServiceProvider.GetService<IStowService>() ?? throw new ServiceNotFoundException(nameof(IStowService));
            _logger = scope.ServiceProvider.GetService<ILogger<StowController>>() ?? throw new ServiceNotFoundException(nameof(ILogger<StowController>));
        }

        [HttpPost("studies")]
        [HttpPost("{workflowName}/studies")]
        [Consumes(ContentTypes.ApplicationDicom, ContentTypes.MultipartRelated)]
        [Produces(ContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(DicomDataset), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(DicomDataset), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(DicomDataset), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(string), StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StoreInstances(string workflowName = "")
        {
            return await StoreInstances(string.Empty, workflowName).ConfigureAwait(false);
        }

        [HttpPost("studies/{studyInstanceUId}")]
        [HttpPost("{workflowName}/studies/{studyInstanceUid}")]
        [Consumes(ContentTypes.ApplicationDicom, ContentTypes.MultipartRelated)]
        [Produces(ContentTypes.ApplicationDicomJson)]
        [ProducesResponseType(typeof(DicomDataset), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(DicomDataset), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(DicomDataset), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(string), StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StoreInstancesToStudy(string studyInstanceUid, string workflowName = "")
        {
            return await StoreInstances(studyInstanceUid, workflowName).ConfigureAwait(false);
        }

        private async Task<IActionResult> StoreInstances(string studyInstanceUid, string workflowName)
        {
            var correlationId = Guid.NewGuid().ToString();
            using var logger = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "CorrelationId", correlationId }, { "StudyInstanceUID", studyInstanceUid }, { "Workflow", workflowName } });

            try
            {
                var result = await _stowService.StoreAsync(Request, studyInstanceUid, workflowName, correlationId, HttpContext.RequestAborted).ConfigureAwait(false);

                return StatusCode(result.StatusCode, result.Data);
            }
            catch (DicomValidationException ex)
            {
                _logger.ErrorDicomWebStowInvalidStudyInstanceUid(studyInstanceUid, ex);
                return StatusCode(
                    StatusCodes.Status400BadRequest,
                    Problem(title: $"Invalid StudyInstanceUID provided '{studyInstanceUid}'.", statusCode: StatusCodes.Status400BadRequest, detail: ex.Message));
            }
            catch (Exception ex)
            {
                _logger.ErrorDicomWebStow(studyInstanceUid, workflowName, ex);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    Problem(title: "Error.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message));
            }
        }
    }
}
