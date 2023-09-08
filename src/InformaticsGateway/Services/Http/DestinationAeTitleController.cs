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
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Scu;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("config/destination")]
    public class DestinationAeTitleController : ControllerBase
    {
        private readonly ILogger<DestinationAeTitleController> _logger;
        private readonly IDestinationApplicationEntityRepository _repository;
        private readonly IDataPlugInEngineFactory<IOutputDataPlugIn> _outputDataPlugInEngineFactory;
        private readonly IScuQueue _scuQueue;

        public DestinationAeTitleController(
            ILogger<DestinationAeTitleController> logger,
            IDestinationApplicationEntityRepository repository,
            IScuQueue scuQueue,
            IDataPlugInEngineFactory<IOutputDataPlugIn> outputDataPlugInEngineFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _scuQueue = scuQueue ?? throw new ArgumentNullException(nameof(scuQueue));
            _outputDataPlugInEngineFactory = outputDataPlugInEngineFactory ?? throw new ArgumentNullException(nameof(outputDataPlugInEngineFactory));
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DestinationApplicationEntity>>> Get()
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
        public async Task<ActionResult<DestinationApplicationEntity>> GetAeTitle(string name)
        {
            try
            {
                var destinationApplicationEntity = await _repository.FindByNameAsync(name, HttpContext.RequestAborted).ConfigureAwait(false);

                if (destinationApplicationEntity is null)
                {
                    return NotFound();
                }

                return Ok(destinationApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorListingDestinationApplicationEntities(ex);
                return Problem(title: "Error querying DICOM destinations.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
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
            var traceId = HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return NotFound();
                }

                var destinationApplicationEntity = await _repository.FindByNameAsync(name, HttpContext!.RequestAborted).ConfigureAwait(false);

                if (destinationApplicationEntity is null)
                {
                    return NotFound();
                }

                var request = new ScuWorkRequest(
                    traceId,
                    RequestType.CEcho,
                    destinationApplicationEntity.HostIp,
                    destinationApplicationEntity.Port,
                    destinationApplicationEntity.AeTitle,
                    HttpContext.RequestAborted
                );
                var response = await _scuQueue.Queue(request, HttpContext.RequestAborted).ConfigureAwait(false);

                if (response.Status != ResponseStatus.Success)
                {
                    return Problem(
                        title: "C-ECHO Failure",
                        instance: traceId,
                        detail: response.Message,
                        statusCode: StatusCodes.Status502BadGateway);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.ErrorCEechoDestinationApplicationEntity(name, ex);
                return Problem(
                    title: $"Error performing C-ECHO",
                    instance: traceId,
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: ex.Message);
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<string>> Create(DestinationApplicationEntity item)
        {
            try
            {
                item.SetDefaultValues();
                item.SetAuthor(User, EditMode.Create);

                await ValidateCreateAsync(item).ConfigureAwait(false);

                await _repository.AddAsync(item, HttpContext.RequestAborted).ConfigureAwait(false);
                _logger.DestinationApplicationEntityAdded(item.AeTitle, item.HostIp);
                return CreatedAtAction(nameof(GetAeTitle), new { name = item.Name }, item);
            }
            catch (ObjectExistsException ex)
            {
                return Problem(title: "DICOM destination already exists", statusCode: StatusCodes.Status409Conflict, detail: ex.Message);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error", statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorAddingDestinationApplicationEntity(ex);
                return Problem(title: "Error adding new DICOM destination", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpPut]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DestinationApplicationEntity>> Edit(DestinationApplicationEntity? item)
        {
            try
            {
                if (item is null)
                {
                    return NotFound();
                }

                var destinationApplicationEntity = await _repository.FindByNameAsync(item.Name, HttpContext.RequestAborted).ConfigureAwait(false);
                if (destinationApplicationEntity is null)
                {
                    return NotFound();
                }

                item.SetDefaultValues();

                destinationApplicationEntity.AeTitle = item.AeTitle;
                destinationApplicationEntity.HostIp = item.HostIp;
                destinationApplicationEntity.Port = item.Port;
                destinationApplicationEntity.SetAuthor(User, EditMode.Update);

                await ValidateUpdateAsync(destinationApplicationEntity).ConfigureAwait(false);

                _ = _repository.UpdateAsync(destinationApplicationEntity, HttpContext.RequestAborted);
                _logger.DestinationApplicationEntityUpdated(item.Name, item.AeTitle, item.HostIp, item.Port);
                return Ok(destinationApplicationEntity);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingDestinationApplicationEntity(ex);
                return Problem(title: "Error updating DICOM destination.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DestinationApplicationEntity>> Delete(string name)
        {
            try
            {
                var destinationApplicationEntity = await _repository.FindByNameAsync(name, HttpContext.RequestAborted).ConfigureAwait(false);
                if (destinationApplicationEntity is null)
                {
                    return NotFound();
                }

                await _repository.RemoveAsync(destinationApplicationEntity, HttpContext.RequestAborted).ConfigureAwait(false);

                _logger.DestinationApplicationEntityDeleted(name);
                return Ok(destinationApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingDestinationApplicationEntity(ex);
                return Problem(title: "Error deleting DICOM destination.", statusCode: StatusCodes.Status500InternalServerError, detail: ex.Message);
            }
        }

        [HttpGet("plug-ins")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<MonaiApplicationEntity> GetPlugIns()
        {
            try
            {
                return Ok(_outputDataPlugInEngineFactory.RegisteredPlugIns());
            }
            catch (Exception ex)
            {
                _logger.ErrorReadingDataInputPlugIns(ex);
                return Problem(title: "Error reading data input plug-ins.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        private async Task ValidateCreateAsync(DestinationApplicationEntity item)
        {
            if (await _repository.ContainsAsync(p => p.Name.Equals(item.Name), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A DICOM destination with the same name '{item.Name}' already exists.");
            }
            if (await _repository.ContainsAsync(p => p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp) && p.Port.Equals(item.Port), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A DICOM destination with the same AE Title '{item.AeTitle}', host/IP Address '{item.HostIp}' and port '{item.Port}' already exists.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }

        private async Task ValidateUpdateAsync(DestinationApplicationEntity item)
        {
            if (await _repository.ContainsAsync(p => !p.Name.Equals(item.Name) && p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp) && p.Port.Equals(item.Port), HttpContext.RequestAborted).ConfigureAwait(false))
            {
                throw new ObjectExistsException($"A DICOM destination with the same AE Title '{item.AeTitle}', host/IP Address '{item.HostIp}' and port '{item.Port}' already exists.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}
