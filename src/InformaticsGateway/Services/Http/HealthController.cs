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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Scp;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IMonaiServiceLocator _monaiServiceLocator;

        public HealthController(
            ILogger<HealthController> logger,
            IMonaiServiceLocator monaiServiceLocator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monaiServiceLocator = monaiServiceLocator ?? throw new ArgumentNullException(nameof(monaiServiceLocator));
        }

        [HttpGet("status")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<HealthStatusResponse> Status()
        {
            try
            {
                var response = new HealthStatusResponse
                {
                    ActiveDimseConnections = ScpService.ActiveConnections,
                    Services = _monaiServiceLocator.GetServiceStatus()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.ErrorCollectingSystemStatus(ex);
                return Problem(title: "Error collecting system status.", statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }
    }
}
