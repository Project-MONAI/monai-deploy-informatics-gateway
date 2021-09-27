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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class HealthControllerTest
    {
        private HealthController _controller;
        private Mock<IMonaiServiceLocator> _serviceLocator;
        private Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private Mock<ILogger<HealthController>> _logger;

        public HealthControllerTest()
        {
            _serviceLocator = new Mock<IMonaiServiceLocator>();
            _logger = new Mock<ILogger<HealthController>>();

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

            _controller = new HealthController(
                 _logger.Object,
                 _serviceLocator.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object
            };
        }

        #region Status

        [RetryFact(5, 250, DisplayName = "Status - Unknown service status")]
        public void Status_ReturnsUnknownStatus()
        {
            _serviceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, ServiceStatus>() { { "Service", ServiceStatus.Unknown } });

            var result = _controller.Status();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as HealthStatusResponse;
            Assert.NotNull(response);
            Assert.Equal(0, response.ActiveDimseConnections);

            foreach (var service in response.Services.Keys)
            {
                Assert.Equal(ServiceStatus.Unknown, response.Services[service]);
            }
        }

        [RetryFact(5, 250, DisplayName = "Status - Shall return actual service status")]
        public void Status_ReturnsActualServiceStatus()
        {
            _serviceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, ServiceStatus>() { { "Service", ServiceStatus.Running } });

            var result = _controller.Status();
            var okObjectResult = result.Result as OkObjectResult;
            var response = okObjectResult.Value as HealthStatusResponse;
            Assert.NotNull(response);
            Assert.Equal(0, response.ActiveDimseConnections);

            foreach (var service in response.Services.Keys)
            {
                Assert.Equal(ServiceStatus.Running, response.Services[service]);
            }
        }

        [RetryFact(5, 250, DisplayName = "Status - Shall return problem on failure")]
        public void Status_ShallReturnProblemOnFailure()
        {
            _serviceLocator.Setup(p => p.GetServiceStatus()).Throws(new Exception("error"));

            var result = _controller.Status();
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error collecting system status.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        }

        #endregion Status

        #region Ready

        [RetryFact(5, 250, DisplayName = "Ready - Shall return Unhealthy")]
        public void Ready_ShallReturnUnhealthy()
        {
            _serviceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, ServiceStatus>() { { "Service", ServiceStatus.Stopped } });

            var readyResult = _controller.Ready();
            var objectResult = readyResult as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal("Unhealthy", objectResult.Value);
            Assert.Equal((int)HttpStatusCode.ServiceUnavailable, objectResult.StatusCode);
        }

        [RetryFact(5, 250, DisplayName = "Ready - Shall return Healthy")]
        public void Ready_ShallReturnHealthy()
        {
            _serviceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, ServiceStatus>() { { "Service", ServiceStatus.Running } });

            var readyResult = _controller.Ready();
            var objectResult = readyResult as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal("Healthy", objectResult.Value);
            Assert.Equal((int)HttpStatusCode.OK, objectResult.StatusCode);
        }

        [RetryFact(5, 250, DisplayName = "Ready - Shall return problem on failure")]
        public void Ready_ShallReturnProblemOnFailure()
        {
            _serviceLocator.Setup(p => p.GetServiceStatus()).Throws(new Exception("error"));

            var result = _controller.Ready();
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.NotNull(problem);
            Assert.Equal("Error collecting system status.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        }

        #endregion Ready
    }
}
