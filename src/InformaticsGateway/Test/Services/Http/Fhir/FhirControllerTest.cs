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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Services.Fhir;
using Monai.Deploy.InformaticsGateway.Services.Http.Fhir;
using Moq;
using Xunit;
using ContentTypes = Monai.Deploy.InformaticsGateway.Services.Fhir.ContentTypes;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http.Fhir
{
    public class FhirControllerTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IFhirService> _fhirService;
        private readonly Mock<ILogger<FhirController>> _logger;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly FhirController _controller;

        public FhirControllerTest()
        {
            _logger = new Mock<ILogger<FhirController>>();
            _fhirService = new Mock<IFhirService>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();

            var services = new ServiceCollection();
            services.AddScoped(p => _fhirService.Object);
            services.AddScoped(p => _serviceScopeFactory.Object);
            services.AddScoped(p => _logger.Object);

            _serviceProvider = services.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            var controllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            _controller = new FhirController(_serviceScopeFactory.Object)
            {
                ControllerContext = controllerContext
            };

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Theory]
        [InlineData(ContentTypes.ApplicationFhirJson)]
        [InlineData(ContentTypes.ApplicationFhirXml)]
        public async Task Create_WhenCalled_ReturnsOriginalResource(string contentType)
        {
            var input = "input data";
            _fhirService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FhirStoreResult
                {
                    RawData = input,
                    StatusCode = StatusCodes.Status201Created
                });

            _controller.Request.ContentType = contentType;

            var result = await _controller.Create();
            Assert.NotNull(result);
            var contentResult = result as ContentResult;
            Assert.NotNull(contentResult);
            Assert.Equal(input, contentResult.Content);
            Assert.Equal(StatusCodes.Status201Created, contentResult.StatusCode);
            Assert.Equal(contentType, contentResult.ContentType);

            _controller.Dispose();
        }

        [Theory]
        [InlineData(ContentTypes.ApplicationFhirJson)]
        [InlineData(ContentTypes.ApplicationFhirXml)]
        public async Task Create_WhenCalledWithResourceType_ReturnsOriginalResource(string contentType)
        {
            var input = "input data";
            _fhirService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FhirStoreResult
                {
                    RawData = input,
                    StatusCode = StatusCodes.Status201Created
                });

            _controller.Request.ContentType = contentType;

            var result = await _controller.Create("Patient");
            Assert.NotNull(result);
            var contentResult = result as ContentResult;
            Assert.NotNull(contentResult);
            Assert.Equal(input, contentResult.Content);
            Assert.Equal(StatusCodes.Status201Created, contentResult.StatusCode);
            Assert.Equal(contentType, contentResult.ContentType);

            _controller.Dispose();
        }

        [Theory]
        [InlineData(ContentTypes.ApplicationFhirJson)]
        [InlineData(ContentTypes.ApplicationFhirXml)]
        public async Task Create_WhenCalledWithResourceTypeWithFhirStoreException_ReturnsFhirOperationOutcome(string contentType)
        {
            _fhirService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FhirStoreException(Guid.NewGuid().ToString(), "error", IssueType.Invalid));

            _controller.Request.ContentType = contentType;

            var result = await _controller.Create("Patient");
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.IsType<OperationOutcome>(objectResult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);

            var operationOutome = objectResult.Value as OperationOutcome;
            Assert.Equal("error", operationOutome.Issues[0].Details[0].Text);
            Assert.Equal(IssueType.Invalid, operationOutome.Issues[0].Code);
            Assert.Equal(IssueSeverity.Error, operationOutome.Issues[0].Severity);

            _controller.Dispose();
        }

        [Theory]
        [InlineData(ContentTypes.ApplicationFhirJson)]
        [InlineData(ContentTypes.ApplicationFhirXml)]
        public async Task Create_WhenCalledWithResourceTypeWithGeneralException_ReturnsFhirOperationOutcome(string contentType)
        {
            _fhirService.Setup(p => p.StoreAsync(It.IsAny<HttpRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("error"));

            _controller.Request.ContentType = contentType;

            var result = await _controller.Create("Patient");
            Assert.NotNull(result);
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.IsType<OperationOutcome>(objectResult.Value);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);

            var operationOutome = objectResult.Value as OperationOutcome;
            Assert.Equal("error", operationOutome.Issues[0].Details[0].Text);
            Assert.Equal(IssueType.Exception, operationOutome.Issues[0].Code);
            Assert.Equal(IssueSeverity.Error, operationOutome.Issues[0].Severity);

            _controller.Dispose();
        }
    }
}
