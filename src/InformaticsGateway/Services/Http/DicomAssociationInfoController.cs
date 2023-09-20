/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Services.UriService;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    [Route("dai")]
    public class DicomAssociationInfoController : PagedApiControllerBase
    {
        private const string Endpoint = "/dai";
        private readonly ILogger<DicomAssociationInfoController> _logger;
        private readonly IDicomAssociationInfoRepository _dicomRepo;
        private readonly IUriService _uriService;

        public DicomAssociationInfoController(ILogger<DicomAssociationInfoController> logger,
            IOptions<InformaticsGatewayConfiguration> options,
            IDicomAssociationInfoRepository dicomRepo,
            IUriService uriService) : base(options)
        {
            _logger = logger;
            _dicomRepo = dicomRepo;
            _uriService = uriService;
        }

        /// <summary>
        /// Gets a paged response list of all workflows.
        /// </summary>
        /// <param name="filter">Filters.</param>
        /// <returns>paged response of subset of all workflows.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<List<DicomAssociationInfo>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync([FromQuery] TimeFilter filter)
        {
            try
            {
                var route = Request?.Path.Value ?? string.Empty;
                var pageSize = filter.PageSize ?? EndpointOptions.Value.EndpointSettings.DefaultPageSize;
                var validFilter = new TimeFilter(
                    filter.StartTime,
                    filter.EndTime,
                    filter.PageNumber ?? 0,
                    pageSize,
                    EndpointOptions.Value.EndpointSettings.MaxPageSize);

                var pagedData = await _dicomRepo.GetAllAsync(
                    validFilter.GetSkip(),
                    validFilter.PageSize,
                    filter.StartTime!.Value,
                    filter.EndTime!.Value, default).ConfigureAwait(false);

                var dataTotal = await _dicomRepo.CountAsync().ConfigureAwait(false);
                var pagedResponse = CreatePagedResponse(pagedData.ToList(), validFilter, dataTotal, _uriService, route);
                return Ok(pagedResponse);
            }
            catch (Exception e)
            {
                _logger.DAIControllerGetAllAsyncError(e);
                return Problem($"Unexpected error occurred: {e.Message}", Endpoint, InternalServerError);
            }
        }
    }
}
