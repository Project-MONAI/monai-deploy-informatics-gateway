// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("config/source")]
    public class SourceAeTitleController : ControllerBase
    {
        private readonly ILogger<SourceAeTitleController> _logger;
        private readonly IInformaticsGatewayRepository<SourceApplicationEntity> _repository;

        public SourceAeTitleController(
            ILogger<SourceAeTitleController> logger,
            IInformaticsGatewayRepository<SourceApplicationEntity> repository)
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
                return Ok(await _repository.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error querying database.");
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
                var sourceApplicationEntity = await _repository.FindAsync(name);

                if (sourceApplicationEntity is null)
                {
                    return NotFound();
                }

                return Ok(sourceApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error querying DICOM sources.");
                return Problem(title: "Error querying DICOM sources.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<string>> Create(SourceApplicationEntity item)
        {
            try
            {
                item.SetDefaultValues();
                Validate(item);

                await _repository.AddAsync(item);
                await _repository.SaveChangesAsync();
                _logger.Log(LogLevel.Information, $"DICOM source added AE Title={item.AeTitle}, Host/IP={item.HostIp}.");
                return CreatedAtAction(nameof(GetAeTitle), new { name = item.Name }, item);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error adding new DICOM source.");
                return Problem(title: "Error adding new DICOM source.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
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
                var sourceApplicationEntity = await _repository.FindAsync(name);
                if (sourceApplicationEntity is null)
                {
                    return NotFound();
                }

                _repository.Remove(sourceApplicationEntity);
                await _repository.SaveChangesAsync();

                _logger.Log(LogLevel.Information, $"DICOM source deleted {name}.");
                return Ok(sourceApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error deleting DICOM source.");
                return Problem(title: "Error deleting DICOM source.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        private void Validate(SourceApplicationEntity item)
        {
            if (_repository.Any(p => p.Name.Equals(item.Name)))
            {
                throw new ConfigurationException($"A DICOM source with the same name '{item.Name}' already exists.");
            }
            if (_repository.Any(p => item.AeTitle.Equals(p.AeTitle) && item.HostIp.Equals(p.HostIp)))
            {
                throw new ConfigurationException($"A DICOM source with the same AE Title '{item.AeTitle}' and host/IP address '{item.HostIp}' already exists.");
            }
            if (!item.IsValid(out IList<string> validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}
