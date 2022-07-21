/*
 * Copyright 2022 MONAI Consortium
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
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Monai.Deploy.InformaticsGateway.Services.Http.DicomWeb;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http.DicomWeb
{
    public class StowControllerTest
    {
        private readonly StowController _controller;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IStowService> _stowService;
        private readonly Mock<ILogger<StowController>> _logger;

        public StowControllerTest()
        {
            _logger = new Mock<ILogger<StowController>>();
            _stowService = new Mock<IStowService>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();

            _problemDetailsFactory = new Mock<ProblemDetailsFactory>();
            _problemDetailsFactory.Setup(_ => _.CreateProblemDetails(
                    It.IsAny<HttpContext>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                )
                .Returns((HttpContext httpContext, int? statusCode, string title, string type, string detail, string instance) =>
                {
                    return new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Type = type,
                        Detail = detail,
                        Instance = instance
                    };
                });

            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider
                .Setup(x => x.GetService(typeof(ILogger<StowController>)))
                .Returns(_logger.Object);
            serviceProvider
                .Setup(x => x.GetService(typeof(IStowService)))
                .Returns(_stowService.Object);

            _serviceScope.SetupGet(p => p.ServiceProvider).Returns(serviceProvider.Object);
            _serviceScopeFactory.Setup(p => p.CreateScope())
                .Returns(_serviceScope.Object);

            var controllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            _controller = new StowController(_serviceScopeFactory.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object,
                ControllerContext = controllerContext
            };
        }

        [Theory(DisplayName = "StoreInstances - returns ProblemDetails on exception")]
        [InlineData("")]
        [InlineData("workflow")]
        public async Task StoreInstances_ReturnsProblemDetailException(string workflow)
        {
            _stowService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            var result = await _controller.StoreInstances(workflow);
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            objectResult = objectResult.Value as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        }

        [Theory(DisplayName = "StoreInstances - returns OK")]
        [InlineData("")]
        [InlineData("workflow")]
        public async Task StoreInstances_ReturnsOk(string workflow)
        {
            _stowService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StowResult
                {
                    StatusCode = StatusCodes.Status200OK,
                    Data = new DicomDataset()
                });

            var result = await _controller.StoreInstances(workflow);
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            var dicomDataset = objectResult.Value as DicomDataset;
            Assert.NotNull(dicomDataset);
            Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
        }

        [Theory(DisplayName = "StoreInstancesToStudy - returns ProblemDetails on bad UID")]
        [InlineData("abc.def", "")]
        [InlineData("a", "workflow")]
        public async Task StoreInstancesToStudy_ReturnsProblemDetailWithInvalidUid(string studyInstanceUid, string workflow)
        {
            _stowService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DicomValidationException("content", DicomVR.SL, "error"));

            var result = await _controller.StoreInstancesToStudy(studyInstanceUid, workflow);
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            objectResult = objectResult.Value as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal($"Invalid StudyInstanceUID provided '{studyInstanceUid}'.", problem.Title);
            Assert.Equal("Content \"content\" does not validate VR SL: error", problem.Detail);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        }

        [Theory(DisplayName = "StoreInstancesToStudy - returns ProblemDetails on exception")]
        [InlineData("1.2.3.4.5", "")]
        [InlineData("1.2.3.4.5", "workflow")]
        public async Task StoreInstancesToStudy_ReturnsProblemDetailException(string studyInstanceUid, string workflow)
        {
            _stowService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            var result = await _controller.StoreInstancesToStudy(studyInstanceUid, workflow);
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            objectResult = objectResult.Value as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        }

        [Theory(DisplayName = "StoreInstancesToStudy - returns OK")]
        [InlineData("1.2.3.4.5", "")]
        [InlineData("1.2.3.4.5", "workflow")]
        public async Task StoreInstancesToStudy_ReturnsOk(string studyInstanceUid, string workflow)
        {
            _stowService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StowResult
                {
                    StatusCode = StatusCodes.Status200OK,
                    Data = new DicomDataset()
                });

            var result = await _controller.StoreInstancesToStudy(studyInstanceUid, workflow);
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            var dicomDataset = objectResult.Value as DicomDataset;
            Assert.NotNull(dicomDataset);
            Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
        }
    }
}
