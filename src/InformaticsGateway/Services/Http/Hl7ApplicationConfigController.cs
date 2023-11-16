/*
 * Copyright 2023 MONAI Consortium
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("configEntity/hl7-application")]
    public class Hl7ApplicationConfigController : ApiControllerBase
    {
        private const string Endpoint = "configEntity/hl7-application";

        private readonly ILogger<Hl7ApplicationConfigController> _logger;
        private readonly IHl7ApplicationConfigRepository _repository;

        public Hl7ApplicationConfigController(ILogger<Hl7ApplicationConfigController> logger, IHl7ApplicationConfigRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpGet]
        [Produces(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<Hl7ApplicationConfigEntity>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var data = await _repository.GetAllAsync().ConfigureAwait(false);
            return Ok(data);
        }

        [HttpGet("{id}")]
        [Produces(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Hl7ApplicationConfigEntity), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(string id)
        {
            var data = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (data == null)
            {
                return NotFound();
            }

            return Ok(data);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(string id)
        {
            var data = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (data == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _repository.DeleteAsync(id).ConfigureAwait(false);
                return Ok(result);
            }
            catch (DatabaseException ex)
            {
                return Problem(title: "Database error removing HL7 Application Configuration.", statusCode: BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(title: "Unknown error removing HL7 Application Configuration.", statusCode: InternalServerError, detail: ex.Message);
            }
        }

        [HttpPut]
        [Consumes(ContentTypes.ApplicationJson)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Put([FromBody] Hl7ApplicationConfigEntity configEntity)
        {
            if (configEntity == null)
            {
                return BadRequest("Hl7ApplicationConfigEntity is null.");
            }

            var errorMessages = configEntity.Validate().ToList();
            if (errorMessages.Any())
            {
                var message = "Hl7ApplicationConfigEntity is invalid. " + string.Join(", ", errorMessages);
                return Problem(title: "Validation Failure.", statusCode: BadRequest, detail: message);
            }

            configEntity.Id = Guid.NewGuid();
            try
            {
                await _repository.CreateAsync(configEntity).ConfigureAwait(false);
                return Ok(configEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.PutHl7ApplicationConfigException(Endpoint, configEntity.ToString(), ex);
                return Problem(title: "Error adding new HL7 Application Configuration.", statusCode: InternalServerError, detail: ex.Message);
            }
        }
    }
}
