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
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Client.Services;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Client.Test
{
    public class AeTitleServiceTest : ServiceTestBase
    {
        private readonly Mock<ILogger> _logger;

        public AeTitleServiceTest()
        {
            _logger = new Mock<ILogger>();
        }

        [Fact(DisplayName = "AE Title - Create")]
        public async Task Create()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                Workflows = new System.Collections.Generic.List<string>() { "A", "B" }
            };

            var json = JsonSerializer.Serialize(aet, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/monaiaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Post, httpResponse);

            var service = new AeTitleService<MonaiApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await service.Create(aet, CancellationToken.None);

            Assert.Equal(aet.AeTitle, result.AeTitle);
            Assert.Equal(aet.Name, result.Name);
            Assert.Equal(aet.Workflows, result.Workflows);
        }

        [Fact(DisplayName = "AE Title - Create returns a problem")]
        public async Task Create_ReturnsAProblem()
        {
            var aet = new MonaiApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                Workflows = new System.Collections.Generic.List<string>() { "A", "B" }
            };

            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/monaiaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Post, httpResponse);

            var service = new AeTitleService<MonaiApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Create(aet, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "AE Title - Update")]
        public async Task Update()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "1.2.3.4"
            };

            var json = JsonSerializer.Serialize(aet, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/source";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Put, httpResponse);

            var service = new AeTitleService<SourceApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await service.Update(aet, CancellationToken.None);

            Assert.Equal(aet.AeTitle, result.AeTitle);
            Assert.Equal(aet.Name, result.Name);
            Assert.Equal(aet.HostIp, result.HostIp);
        }

        [Fact(DisplayName = "AE Title - Update returns a problem")]
        public async Task Update_ReturnsAProblem()
        {
            var aet = new DestinationApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "host",
                Port = 123
            };

            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/destination";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Put, httpResponse);

            var service = new AeTitleService<DestinationApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Update(aet, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "AE Title - Get")]
        public async Task Get()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "1.2.3.4"
            };

            var json = JsonSerializer.Serialize(aet, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/sourceaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new AeTitleService<SourceApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await service.GetAeTitle(aet.Name, CancellationToken.None);

            Assert.Equal(aet.AeTitle, result.AeTitle);
            Assert.Equal(aet.Name, result.Name);
            Assert.Equal(aet.HostIp, result.HostIp);
        }

        [Fact(DisplayName = "AE Title - Get returns a problem")]
        public async Task Get_ReturnsAProblem()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "1.2.3.4"
            };

            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/sourceaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new AeTitleService<SourceApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.GetAeTitle(aet.Name, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "AE Title - Delete")]
        public async Task Delete()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "1.2.3.4"
            };

            var json = JsonSerializer.Serialize(aet, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/sourceaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Delete, httpResponse);

            var service = new AeTitleService<SourceApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await service.Delete(aet.Name, CancellationToken.None);

            Assert.Equal(aet.AeTitle, result.AeTitle);
            Assert.Equal(aet.Name, result.Name);
            Assert.Equal(aet.HostIp, result.HostIp);
        }

        [Fact(DisplayName = "AE Title - Delete returns a problem")]
        public async Task Delete_ReturnsAProblem()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "1.2.3.4"
            };

            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/sourceaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Delete, httpResponse);

            var service = new AeTitleService<SourceApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Delete(aet.Name, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "AE Title - List")]
        public async Task List()
        {
            var list = new List<DestinationApplicationEntity>() {
                new DestinationApplicationEntity()
                {
                    AeTitle = "Test1",
                    Name = "Test Name 1",
                    HostIp = "1.1.1.1",
                    Port = 104
                },
                new DestinationApplicationEntity()
                {
                    AeTitle = "Test2",
                    Name = "Test Name 2",
                    HostIp = "2.2.2.2",
                    Port = 204
                }
            };

            var json = JsonSerializer.Serialize(list, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/destinationaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new AeTitleService<DestinationApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await service.List(CancellationToken.None);

            Assert.Equal(list.Count, result.Count);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i].Name, result[i].Name);
                Assert.Equal(list[i].AeTitle, result[i].AeTitle);
                Assert.Equal(list[i].HostIp, result[i].HostIp);
                Assert.Equal(list[i].Port, result[i].Port);
            }
        }

        [Fact(DisplayName = "AE Title - List returns a problem")]
        public async Task List_ReturnsAProblem()
        {
            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/destinationaetitle";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new AeTitleService<SourceApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.List(CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "Destination - C-ECHO")]
        public async Task CEcho()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test",
                HostIp = "1.2.3.4"
            };

            var json = JsonSerializer.Serialize(aet, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/destination";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new AeTitleService<DestinationApplicationEntity>(uriPath, httpClient, _logger.Object);

            var exception = await Record.ExceptionAsync(async () => await service.CEcho(aet.Name, CancellationToken.None));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "Destination - C-ECHO returns a problem")]
        public async Task CEcho_ReturnsAProblem()
        {
            var aet = new SourceApplicationEntity()
            {
                AeTitle = "Test",
                Name = "Test Name",
                HostIp = "1.2.3.4"
            };

            var problem = new ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonSerializer.Serialize(problem, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");
            var uriPath = "config/destination";

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new AeTitleService<DestinationApplicationEntity>(uriPath, httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.CEcho(aet.Name, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }
    }
}
