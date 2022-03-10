// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
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
    [Route("config/destination")]
    public class DestinationAeTitleController : ControllerBase
    {
        private readonly ILogger<DestinationAeTitleController> _logger;
        private readonly IInformaticsGatewayRepository<DestinationApplicationEntity> _repository;

        public DestinationAeTitleController(
            ILogger<DestinationAeTitleController> logger,
            IInformaticsGatewayRepository<DestinationApplicationEntity> repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DestinationApplicationEntity>>> Get()
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
        [ActionName(nameof(GetAeTitle))]
        public async Task<ActionResult<DestinationApplicationEntity>> GetAeTitle(string name)
        {
            try
            {
                var destinationApplicationEntity = await _repository.FindAsync(name);

                if (destinationApplicationEntity is null)
                {
                    return NotFound();
                }

                return Ok(destinationApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error querying DICOM destinations.");
                return Problem(title: "Error querying DICOM destinations.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<string>> Create(DestinationApplicationEntity item)
        {
            try
            {
                item.SetDefaultValues();

                Validate(item);

                await _repository.AddAsync(item);
                await _repository.SaveChangesAsync();
                _logger.Log(LogLevel.Information, $"DICOM destination added AE Title={item.AeTitle}, Host/IP={item.HostIp}.");
                return CreatedAtAction(nameof(GetAeTitle), new { name = item.Name }, item);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error adding new DICOM destination.");
                return Problem(title: "Error adding new DICOM destination.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
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
                var destinationApplicationEntity = await _repository.FindAsync(name);
                if (destinationApplicationEntity is null)
                {
                    return NotFound();
                }

                _repository.Remove(destinationApplicationEntity);
                await _repository.SaveChangesAsync();

                _logger.Log(LogLevel.Information, $"DICOM destination deleted {name}.");
                return Ok(destinationApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error deleting DICOM destination.");
                return Problem(title: "Error deleting DICOM destination.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        private void Validate(DestinationApplicationEntity item)
        {
            if (_repository.Any(p => p.Name.Equals(item.Name)))
            {
                throw new ConfigurationException($"A DICOM destination with the same name '{item.Name}' already exists.");
            }
            if (_repository.Any(p => p.AeTitle.Equals(item.AeTitle) && p.HostIp.Equals(item.HostIp) && p.Port.Equals(item.Port)))
            {
                throw new ConfigurationException($"A DICOM destination with the same AE Title '{item.AeTitle}', host/IP Address '{item.HostIp}' and port '{item.Port}' already exists.");
            }
            if (!item.IsValid(out IList<string> validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}
