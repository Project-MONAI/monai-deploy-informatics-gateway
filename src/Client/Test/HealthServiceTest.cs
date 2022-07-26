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
