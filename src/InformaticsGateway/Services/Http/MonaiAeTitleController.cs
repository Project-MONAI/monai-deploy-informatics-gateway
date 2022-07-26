/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.Net.Mime;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Scp;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("config/ae")]
    public class MonaiAeTitleController : ControllerBase
    {
        private readonly ILogger<MonaiAeTitleController> _logger;
        private readonly IInformaticsGatewayRepository<MonaiApplicationEntity> _repository;
        private readonly IMonaiAeChangedNotificationService _monaiAeChangedNotificationService;

        public MonaiAeTitleController(
            ILogger<MonaiAeTitleController> logger,
            IMonaiAeChangedNotificationService monaiAeChangedNotificationService,
            IInformaticsGatewayRepository<MonaiApplicationEntity> repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _monaiAeChangedNotificationService = monaiAeChangedNotificationService ?? throw new ArgumentNullException(nameof(monaiAeChangedNotificationService));
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<MonaiApplicationEntity>>> Get()
        {
            try
            {
                return Ok(await _repository.ToListAsync().ConfigureAwait(false));
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
        public async Task<ActionResult<MonaiApplicationEntity>> GetAeTitle(string name)
        {
            try
            {
                var monaiApplicationEntity = await _repository.FindAsync(name).ConfigureAwait(false);

                if (monaiApplicationEntity is null)
                {
                    return NotFound();
                }

                return Ok(monaiApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorListingMonaiApplicationEntities(ex);
                return Problem(title: "Error querying MONAI Application Entity.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<MonaiApplicationEntity>> Create(MonaiApplicationEntity item)
        {
            try
            {
                Validate(item);

                item.SetDefaultValues();

                await _repository.AddAsync(item).ConfigureAwait(false);
                await _repository.SaveChangesAsync().ConfigureAwait(false);
                _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(item, ChangedEventType.Added));
                _logger.MonaiApplicationEntityAdded(item.AeTitle);
                return CreatedAtAction(nameof(GetAeTitle), new { name = item.Name }, item);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.ErrorAddingMonaiApplicationEntity(ex);
                return Problem(title: "Error adding new MONAI Application Entity.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpDelete("{name}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MonaiApplicationEntity>> Delete(string name)
        {
            try
            {
                var monaiApplicationEntity = await _repository.FindAsync(name).ConfigureAwait(false);
                if (monaiApplicationEntity is null)
                {
                    return NotFound();
                }

                _repository.Remove(monaiApplicationEntity);
                await _repository.SaveChangesAsync().ConfigureAwait(false);

                _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(monaiApplicationEntity, ChangedEventType.Deleted));
                _logger.MonaiApplicationEntityDeleted(name);
                return Ok(monaiApplicationEntity);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeletingMonaiApplicationEntity(ex);
                return Problem(title: "Error deleting MONAI Application Entity.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        private void Validate(MonaiApplicationEntity item)
        {
            Guard.Against.Null(item, nameof(item));

            if (_repository.Any(p => p.Name.Equals(item.Name)))
            {
                throw new ConfigurationException($"A MONAI Application Entity with the same name '{item.Name}' already exists.");
            }
            if (_repository.Any(p => p.AeTitle.Equals(item.AeTitle)))
            {
                throw new ConfigurationException($"A MONAI Application Entity with the same AE Title '{item.AeTitle}' already exists.");
            }
            if (item.IgnoredSopClasses.Any() && item.AllowedSopClasses.Any())
            {
                throw new ConfigurationException($"Cannot specify both allowed and ignored SOP classes at the same time, they are mutually exclusive.");
            }
            if (!item.IsValid(out var validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}
