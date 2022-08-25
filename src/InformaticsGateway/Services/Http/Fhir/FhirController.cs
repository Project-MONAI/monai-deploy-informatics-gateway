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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Fhir;

namespace Monai.Deploy.InformaticsGateway.Services.Http.Fhir
{
    [ApiController]
    [Route("fhir")]
    public class FhirController : ControllerBase, IDisposable
    {
        private readonly IServiceScope _scope;
        private readonly IFhirService _fhirService;
        private readonly ILogger<FhirController> _logger;
        private bool _disposedValue;

        public FhirController(IServiceScopeFactory serviceScopeFactory)
        {
            if (serviceScopeFactory is null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            }

            _scope = serviceScopeFactory.CreateScope();
            _fhirService = _scope.ServiceProvider.GetService<IFhirService>() ?? throw new ServiceNotFoundException(nameof(IFhirService));
            _logger = _scope.ServiceProvider.GetService<ILogger<FhirController>>() ?? throw new ServiceNotFoundException(nameof(ILogger<FhirController>));
        }

        [HttpPost]
        [Consumes(ContentTypes.ApplicationFhirJson, ContentTypes.ApplicationFhirXml)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Produces("application/fhir+json")]
        public async Task<IActionResult> Create()
        {
            return await Create(string.Empty).ConfigureAwait(false);
        }

        [HttpPost]
        [Consumes(ContentTypes.ApplicationFhirJson, ContentTypes.ApplicationFhirXml)]
        [Route($"{{{Resources.RouteNameResourceType}:{Resources.ResourceTypeRouteConstraint}}}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OperationOutcome), StatusCodes.Status500InternalServerError)]
        [Produces(ContentTypes.ApplicationFhirJson, ContentTypes.ApplicationFhirXml)]
        public async Task<IActionResult> Create(string resourceType)
        {
            var correlationId = Guid.NewGuid().ToString();
            using var logger = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "CorrelationId", correlationId } });

            try
            {
                var result = await _fhirService.StoreAsync(Request, correlationId, resourceType, HttpContext.RequestAborted).ConfigureAwait(false);
                return new ContentResult
                {
                    Content = result.RawData,
                    ContentType = Request.ContentType,
                    StatusCode = result.StatusCode
                };
            }
            catch (FhirStoreException ex)
            {
                _logger.FhirStoreException(ex);
                return new ObjectResult(ex.OperationOutcome)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorStoringFhirResource(ex);
                return OperatorionOutcome(correlationId, ex, StatusCodes.Status500InternalServerError);
            }
        }

        private static IActionResult OperatorionOutcome(string correlationId, Exception exception, int httpStatusCode)
        {
            var operationOutput = new OperationOutcome
            {
                Id = correlationId,
                ResourceType = Resources.ResourceOperationOutcome,
            };

            operationOutput.Issues.Add(new Issue
            {
                Severity = IssueSeverity.Error,
                Code = IssueType.Exception
            });

            operationOutput.Issues[0].Details.Add(new IssueDetails
            {
                Text = exception.Message
            });

            return new ObjectResult(operationOutput)
            {
                StatusCode = httpStatusCode
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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
