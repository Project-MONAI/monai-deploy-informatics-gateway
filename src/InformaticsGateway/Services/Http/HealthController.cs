// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Linq;
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

        [HttpGet("ready")]
        [HttpGet("live")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult Ready()
        {
            try
            {
                var services = _monaiServiceLocator.GetServiceStatus();

                if (services.Values.Any((p) => p != ServiceStatus.Running))
                {
                    return StatusCode((int)HttpStatusCode.ServiceUnavailable, "Unhealthy");
                }

                return Ok("Healthy");
            }
            catch (Exception ex)
            {
                _logger.ErrorCollectingSystemStatus(ex);
                return Problem(title: "Error collecting system status.", statusCode: (int)HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }
    }
}
