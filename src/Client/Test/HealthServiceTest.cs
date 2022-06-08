// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Client.Services;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Client.Test
{
    public class HealthServiceTest : ServiceTestBase
    {
        private readonly Mock<ILogger> _logger;

        public HealthServiceTest()
        {
            _logger = new Mock<ILogger>();
        }

        [Fact(DisplayName = "Health - Status")]
        public async Task Status()
        {
            var status = new HealthStatusResponse()
            {
                ActiveDimseConnections = 1,
                Services = new Dictionary<string, ServiceStatus>() { { "a", ServiceStatus.Running } }
            };

            var json = JsonSerializer.Serialize(status, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new HealthService(httpClient, _logger.Object);

            var result = await service.Status(CancellationToken.None);

            Assert.Equal(status.ActiveDimseConnections, result.ActiveDimseConnections);
            Assert.Equal(status.Services, result.Services);
        }

        [Fact(DisplayName = "Health - Status returns a problem")]
        public async Task Status_ReturnsAProblem()
        {
            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new HealthService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Status(CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "Health - Live")]
        public async Task Live()
        {
            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Healthy", Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new HealthService(httpClient, _logger.Object);

            var result = await service.Live(CancellationToken.None);

            Assert.Equal("Healthy", result);
        }

        [Fact(DisplayName = "Health - Live returns a problem")]
        public async Task Live_ReturnsAProblem()
        {
            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new HealthService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Live(CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "Health - Ready")]
        public async Task Ready()
        {
            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Healthy", Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new HealthService(httpClient, _logger.Object);

            var result = await service.Ready(CancellationToken.None);

            Assert.Equal("Healthy", result);
        }

        [Fact(DisplayName = "Health - Ready returns a problem")]
        public async Task Ready_ReturnsAProblem()
        {
            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new HealthService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Ready(CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }
    }
}
