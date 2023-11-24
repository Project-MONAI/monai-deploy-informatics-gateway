/*
 * Copyright 2021-2023 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("config/hl7-destination")]
    public class HL7DestinationController : ControllerBase
    {
        private readonly ILogger<HL7DestinationController> _logger;
        private readonly IHL7DestinationEntityRepository _repository;

        public HL7DestinationController(
            ILogger<HL7DestinationController> logger,
            IHL7DestinationEntityRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<HL7DestinationEntity>>> Get()
        {
            try
            {
                return Ok(await _repository.ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _logger.ErrorQueryingDatabase(ex);
                return Problem(title: "Error querying database.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ActionName(nameof(GetAeTitle))]
        public async Task<ActionResult<HL7DestinationEntity>> GetAeTitle(string name)
        {
            try
            {
                var hl7DestinationEntity = await _repository.FindByNameAsync(name, HttpContext.RequestAborted).ConfigureAwait(false);

                if (hl7DestinationEntity is null)
                {
                    return NotFound();
                }

                return Ok(hl7DestinationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorListingHL7DestinationEntities(ex);
                return Problem(title: "Error querying HL7 destinations.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpGet("cecho/{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ActionName(nameof(GetAeTitle))]
        public async Task<IActionResult> CEcho(string name)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<string>> Create(HL7DestinationEntity item)
        {
            try
            {
                item.SetDefaultValues();
                item.SetAuthor(User, EditMode.Create);

                await ValidateCreateAsync(item).ConfigureAwait(false);

                await _repository.AddAsync(item, HttpContext.RequestAborted).ConfigureAwait(false);
                _logger.HL7DestinationEntityAdded(item.AeTitle, item.HostIp);
                return CreatedAtAction(nameof(GetAeTitle), new { name = item.Name }, item);
            }
            catch (ObjectExistsException ex)
            {
                return Problem(title: "HL7 destination already exists", statusCode: StatusCodes.Status409Conflict, detail: ex.Message);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error", statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorAddingHL7DestinationEntity(ex);
                return Problem(title: "Error adding new HL7 destination", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpPut]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HL7DestinationEntity>> Edit(HL7DestinationEntity? item)
        {
            try
            {
                if (item is null)
                {
                    return NotFound();
                }

                var hl7DestinationEntity = await _repository.FindByNameAsync(item.Name, HttpContext.RequestAborted).ConfigureAwait(false);
                if (hl7DestinationEntity is null)
                {
                    return NotFound();
                }

                item.SetDefaultValues();

                hl7DestinationEntity.AeTitle = item.AeTitle;
                hl7DestinationEntity.HostIp = item.HostIp;
                hl7DestinationEntity.Port = item.Port;
                hl7DestinationEntity.SetAuthor(User, EditMode.Update);

                await ValidateUpdateAsync(hl7DestinationEntity).ConfigureAwait(false);

                _ = _repository.UpdateAsync(hl7DestinationEntity, HttpContext.RequestAborted);
                _logger.HL7DestinationEntityUpdated(item.Name, item.AeTitle, item.HostIp, item.Port);
                return Ok(hl7DestinationEntity);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingHL7DestinationEntity(ex);
                return Problem(title: "Error updating HL7 destination.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HL7DestinationEntity>> Delete(string name)
        {
            try
            {
                var hl7DestinationEntity = await _repository.FindByNameAsync(name, HttpContext.RequestAborted).ConfigureAwait(false);
                if (hl7DestinationEntity is null)
                {
                    return NotFound();
                }

                await _repository.RemoveAsync(hl7DestinationEntity, HttpContext.RequestAborted).ConfigureAwait(false);

                _logger.HL7DestinationEntityDeleted(name.Substring(0, 10));
                return Ok(hl7DestinationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingHL7DestinationEntity(ex);
                return Problem(title: "Error deleting HL7 destination.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        private async Task ValidateCreateAsync(HL7DestinationEntity item)
        {
            if (await _repository.ContainsAsync(p => p.Name.Equals(item.Name), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A HL7 destination with the same name '{item.Name}' already exists.");
            }
            if (await _repository.ContainsAsync(p => p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp) && p.Port.Equals(item.Port), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A HL7 destination with the same AE Title '{item.AeTitle}', host/IP Address '{item.HostIp}' and port '{item.Port}' already exists.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }

        private async Task ValidateUpdateAsync(HL7DestinationEntity item)
        {
            if (await _repository.ContainsAsync(p => !p.Name.Equals(item.Name) && p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp) && p.Port.Equals(item.Port), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A HL7 destination with the same AE Title '{item.AeTitle}', host/IP Address '{item.HostIp}' and port '{item.Port}' already exists.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}
