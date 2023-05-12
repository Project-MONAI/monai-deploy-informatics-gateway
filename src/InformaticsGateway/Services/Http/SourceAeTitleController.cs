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
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("config/source")]
    public class SourceAeTitleController : ControllerBase
    {
        private readonly ILogger<SourceAeTitleController> _logger;
        private readonly ISourceApplicationEntityRepository _repository;

        public SourceAeTitleController(
            ILogger<SourceAeTitleController> logger,
            ISourceApplicationEntityRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SourceApplicationEntity>>> Get()
        {
            try
            {
                return Ok(await _repository.ToListAsync(HttpContext.RequestAborted).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                _logger.ErrorQueryingDatabase(ex);
                return Problem(title: "Error querying database.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpGet("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ActionName(nameof(GetAeTitle))]
        public async Task<ActionResult<SourceApplicationEntity>> GetAeTitle(string name)
        {
            try
            {
                var sourceApplicationEntity = await _repository.FindByNameAsync(name, HttpContext.RequestAborted).ConfigureAwait(false);

                if (sourceApplicationEntity is null)
                {
                    return NotFound();
                }

                return Ok(sourceApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorListingSourceApplicationEntities(ex);
                return Problem(title: "Error querying DICOM sources.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpGet("/aetitle/{aeTitle}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ActionName(nameof(GetAeTitleByAET))]
        public async Task<ActionResult<SourceApplicationEntity>> GetAeTitleByAET(string aeTitle)
        {
            try
            {
                var sourceApplicationEntity = await _repository.FindByAETAsync(aeTitle, HttpContext.RequestAborted).ConfigureAwait(false);

                if (sourceApplicationEntity is null)
                {
                    return NotFound();
                }

                return Ok(sourceApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorListingSourceApplicationEntities(ex);
                return Problem(title: "Error querying DICOM sources.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<string>> Create(SourceApplicationEntity item)
        {
            try
            {
                item.SetDefaultValues();
                item.SetAuthor(User, EditMode.Create);
                await ValidateCreateAsync(item).ConfigureAwait(false);

                await _repository.AddAsync(item, HttpContext.RequestAborted).ConfigureAwait(false);
                _logger.SourceApplicationEntityAdded(item.AeTitle, item.HostIp);
                return CreatedAtAction(nameof(GetAeTitle), new { name = item.Name }, item);
            }
            catch (ObjectExistsException ex)
            {
                return Problem(title: "DICOM source already exists", statusCode: (int)System.Net.HttpStatusCode.Conflict, detail: ex.Message);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorAddingSourceApplicationEntity(ex);
                return Problem(title: "Error adding new DICOM source.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPut]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SourceApplicationEntity>> Edit(SourceApplicationEntity item)
        {
            try
            {
                if (item is null)
                {
                    return NotFound();
                }

                var sourceApplicationEntity = await _repository.FindByNameAsync(item.Name, HttpContext.RequestAborted).ConfigureAwait(false);
                if (sourceApplicationEntity is null)
                {
                    return NotFound();
                }

                item.SetDefaultValues();

                sourceApplicationEntity.AeTitle = item.AeTitle;
                sourceApplicationEntity.HostIp = item.HostIp;
                sourceApplicationEntity.SetAuthor(User, EditMode.Update);

                await ValidateEditAsync(sourceApplicationEntity).ConfigureAwait(false);

                await _repository.UpdateAsync(sourceApplicationEntity, HttpContext.RequestAborted).ConfigureAwait(false);
                _logger.SourceApplicationEntityUpdated(item.Name, item.AeTitle, item.HostIp);
                return Ok(sourceApplicationEntity);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingDestinationApplicationEntity(ex);
                return Problem(title: "Error updating DICOM source.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SourceApplicationEntity>> Delete(string name)
        {
            try
            {
                var sourceApplicationEntity = await _repository.FindByNameAsync(name, HttpContext.RequestAborted).ConfigureAwait(false);
                if (sourceApplicationEntity is null)
                {
                    return NotFound();
                }

                await _repository.RemoveAsync(sourceApplicationEntity, HttpContext.RequestAborted).ConfigureAwait(false);

                _logger.SourceApplicationEntityDeleted(name);
                return Ok(sourceApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingSourceApplicationEntity(ex);
                return Problem(title: "Error deleting DICOM source.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        private async Task ValidateCreateAsync(SourceApplicationEntity item)
        {
            if (await _repository.ContainsAsync(p => p.Name.Equals(item.Name), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A DICOM source with the same name '{item.Name}' already exists.");
            }
            if (await _repository.ContainsAsync(p => p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A DICOM source with the same AE Title '{item.AeTitle}' and host/IP address '{item.HostIp}' already exists.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }

        private async Task ValidateEditAsync(SourceApplicationEntity item)
        {
            if (await _repository.ContainsAsync(p => !p.Name.Equals(item.Name) && p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A DICOM source with the same AE Title '{item.AeTitle}' and host/IP address '{item.HostIp}' already exists.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}
