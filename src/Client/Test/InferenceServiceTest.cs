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
    public class InferenceServiceTest : ServiceTestBase
    {
        private readonly Mock<ILogger> _logger;

        public InferenceServiceTest()
        {
            _logger = new Mock<ILogger>();
        }

        [Fact(DisplayName = "Inference - New")]
        public async Task New()
        {
            var inferenceRequest = new InferenceRequest()
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(inferenceRequest, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Post, httpResponse);

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await service.NewInferenceRequest(inferenceRequest, CancellationToken.None);

            Assert.Equal(inferenceRequest.TransactionId, result.TransactionId);
        }

        [Fact(DisplayName = "Inference - New returns a problem")]
        public async Task New_ReturnsAProblem()
        {
            var inferenceRequest = new InferenceRequest()
            {
                TransactionId = Guid.NewGuid().ToString()
            };

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

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Post, httpResponse);

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.NewInferenceRequest(inferenceRequest, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }

        [Fact(DisplayName = "Inference - Status")]
        public async Task Status()
        {
            var inferenceRequest = new InferenceRequest()
            {
                TransactionId = Guid.NewGuid().ToString()
            };
            var inferenceStatus = new InferenceStatusResponse()
            {
                TransactionId = inferenceRequest.TransactionId
            };
            var json = JsonSerializer.Serialize(inferenceStatus, Configuration.JsonSerializationOptions);

            var rootUri = new Uri("http://localhost:5000");

            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = SetupHttpClientMock(rootUri, HttpMethod.Get, httpResponse);

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await service.Status(inferenceRequest.TransactionId, CancellationToken.None);

            Assert.Equal(inferenceRequest.TransactionId, result.TransactionId);
        }

        [Fact(DisplayName = "Inference - Status returns a problem")]
        public async Task Status_ReturnsAProblem()
        {
            var inferenceRequest = new InferenceRequest()
            {
                TransactionId = Guid.NewGuid().ToString()
            };

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

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Status(inferenceRequest.TransactionId, CancellationToken.None));

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }
    }
}
