using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
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
        private Mock<ILoggerFactory> _loggerFactory;
        private readonly DicomAssociationInfoController _controller;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly Mock<IDicomAssociationInfoRepository> _repo;
        private readonly UriService _uriService;

        public DicomAssociationInfoControllerTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<DicomAssociationInfoController>>();
            _repo = new Mock<IDicomAssociationInfoRepository>();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _options = Options.Create(new InformaticsGatewayConfiguration());
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
            Assert.Equal(0 ,response.TotalRecords);
            Assert.Empty(response.Data);
        }
    }
}
