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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common.Pagination;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.InformaticsGateway.Services.UriService;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class DicomAssociationInfoControllerTest
    {
        private readonly Mock<ILogger<DicomAssociationInfoController>> _logger;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly DicomAssociationInfoController _controller;
        private readonly IOptions<HttpEndpointSettings> _options;
        private readonly Mock<IDicomAssociationInfoRepository> _repo;
        private readonly UriService _uriService;

        public DicomAssociationInfoControllerTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<DicomAssociationInfoController>>();
            _repo = new Mock<IDicomAssociationInfoRepository>();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _options = Options.Create(new HttpEndpointSettings());
            _uriService = new UriService(new Uri("https://test.com/"));

            _controller = new DicomAssociationInfoController(_logger.Object, _options, _repo.Object, _uriService);
        }

        [Fact]
        public async Task GetAllAsync_GiveExpectedInput_ReturnsOK()
        {
            var input = new TimeFilter
            {
                EndTime = DateTime.Now,
                StartTime = DateTime.MinValue,
                PageNumber = 0,
                PageSize = 1
            };
            _repo.Setup(r => r.GetAllAsync(It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DicomAssociationInfo>());
            var result = await _controller.GetAllAsync(input);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PagedResponse<IEnumerable<DicomAssociationInfo>>>(okResult.Value);
            Assert.Equal(0, response.TotalRecords);
            Assert.Empty(response.Data);
        }
    }
}
