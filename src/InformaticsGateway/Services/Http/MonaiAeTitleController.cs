// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [ApiController]
    [Route("config/[controller]")]
    public class MonaiAeTitleController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonaiAeTitleController> _logger;
        private readonly IInformaticsGatewayRepository<MonaiApplicationEntity> _repository;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IMonaiAeChangedNotificationService _monaiAeChangedNotificationService;
        private ConfigurationValidator _configurationValidator;

        public MonaiAeTitleController(
            IServiceProvider serviceProvider,
            ILogger<MonaiAeTitleController> logger,
            ConfigurationValidator configurationValidator,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IMonaiAeChangedNotificationService monaiAeChangedNotificationService,
            IInformaticsGatewayRepository<MonaiApplicationEntity> repository)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _configurationValidator = configurationValidator ?? throw new ArgumentNullException(nameof(configurationValidator));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
                return await _repository.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error querying database.");
                return Problem(title: "Error querying database.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpGet("{aeTitle}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ActionName(nameof(GetAeTitle))]
        public async Task<ActionResult<MonaiApplicationEntity>> GetAeTitle(string aeTitle)
        {
            try
            {
                var monaiApplicationEntity = await _repository.FindAsync(aeTitle);

                if (monaiApplicationEntity is null)
                {
                    return NotFound();
                }

                return monaiApplicationEntity;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error querying MONAI Application Entity.");
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

                await _repository.AddAsync(item);
                await _repository.SaveChangesAsync();
                _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(item, ChangedEventType.Added));
                _logger.Log(LogLevel.Information, $"MONAI SCP AE Title added AE Title={item.AeTitle}.");
                return CreatedAtAction(nameof(GetAeTitle), new { aeTitle = item.AeTitle }, item);
            }
            catch (ConfigurationException ex)
            {
                return Problem(title: "Validation error.", statusCode: (int)System.Net.HttpStatusCode.BadRequest, detail: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error adding new MONAI Application Entity.");
                return Problem(title: "Error adding new MONAI Application Entity.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        [HttpDelete("{aeTitle}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MonaiApplicationEntity>> Delete(string aeTitle)
        {
            try
            {
                var monaiApplicationEntity = await _repository.FindAsync(aeTitle);
                if (monaiApplicationEntity is null)
                {
                    return NotFound();
                }

                _repository.Remove(monaiApplicationEntity);
                await _repository.SaveChangesAsync();

                _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(monaiApplicationEntity, ChangedEventType.Deleted));
                _logger.Log(LogLevel.Information, $"MONAI SCP AE Title deleted AE Title={aeTitle}.");
                return monaiApplicationEntity;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Error deleting MONAI Application Entity.");
                return Problem(title: "Error deleting MONAI Application Entity.", statusCode: (int)System.Net.HttpStatusCode.InternalServerError, detail: ex.Message);
            }
        }

        private void Validate(MonaiApplicationEntity item)
        {
            Guard.Against.Null(item, nameof(item));

            if (!item.IsValid(_repository.AsQueryable().Select(p => p.AeTitle), out IList<string> validationErrors))
            {
                throw new ConfigurationException(string.Join(Environment.NewLine, validationErrors));
            }
        }
    }
}