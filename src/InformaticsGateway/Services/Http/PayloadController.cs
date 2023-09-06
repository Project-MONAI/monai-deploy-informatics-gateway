using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("payload")]
    public class PayloadController : ControllerBase
    {
        private readonly ILogger<PayloadController> _logger;
        private readonly IPayloadRepository _repository;


        public PayloadController(
            ILogger<PayloadController> logger,
            IPayloadRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<MonaiApplicationEntity>>> Get()
        {
            try
            {
                var payloads = await _repository.ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false);
                return Ok(payloads.Select(payload => new PayloadDTO(payload)));
            }
            catch (Exception ex)
            {
                _logger.ErrorQueryingDatabase(ex);
                return Problem(title: "Error querying database.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }
    }
}
