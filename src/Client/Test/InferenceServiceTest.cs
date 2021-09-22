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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Client.Services;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            var json = JsonConvert.SerializeObject(inferenceRequest);

            string rootUri = "http://localhost:5000";
            string uriPath = "inference";

            var httpResponse = new HttpResponseMessage();
            httpResponse.StatusCode = HttpStatusCode.OK;
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpClient = SetupHttpClientMock(rootUri, $"{rootUri}/{uriPath}", HttpMethod.Post, httpResponse);

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await service.New(inferenceRequest, CancellationToken.None);

            Assert.Equal(inferenceRequest.TransactionId, result.TransactionId);
        }

        [Fact(DisplayName = "Inference - New returns a problem")]
        public async Task New_ReturnsAProblem()
        {
            var inferenceRequest = new InferenceRequest()
            {
                TransactionId = Guid.NewGuid().ToString()
            };

            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonConvert.SerializeObject(problem);

            string rootUri = "http://localhost:5000";
            string uriPath = "inference";

            var httpResponse = new HttpResponseMessage();
            httpResponse.StatusCode = HttpStatusCode.InternalServerError;
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpClient = SetupHttpClientMock(rootUri, $"{rootUri}/{uriPath}", HttpMethod.Post, httpResponse);

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.New(inferenceRequest, CancellationToken.None));

            _logger.VerifyLogging("Error sending request", LogLevel.Error, Times.Once());

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
            var json = JsonConvert.SerializeObject(inferenceStatus);

            string rootUri = "http://localhost:5000";
            string uriPath = "inference";

            var httpResponse = new HttpResponseMessage();
            httpResponse.StatusCode = HttpStatusCode.OK;
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpClient = SetupHttpClientMock(rootUri, $"{rootUri}/{uriPath}/status", HttpMethod.Get, httpResponse);

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

            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Problem Title",
                Detail = "Problem Detail",
                Status = 500
            };

            var json = JsonConvert.SerializeObject(problem);

            string rootUri = "http://localhost:5000";
            string uriPath = "inference";

            var httpResponse = new HttpResponseMessage();
            httpResponse.StatusCode = HttpStatusCode.InternalServerError;
            httpResponse.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpClient = SetupHttpClientMock(rootUri, $"{rootUri}/{uriPath}/status", HttpMethod.Get, httpResponse);

            var service = new InferenceService(httpClient, _logger.Object);

            var result = await Assert.ThrowsAsync<ProblemException>(async () => await service.Status(inferenceRequest.TransactionId, CancellationToken.None));

            _logger.VerifyLogging("Error sending request", LogLevel.Error, Times.Once());

            Assert.Equal($"HTTP Status: {problem.Status}. {problem.Detail}", result.Message);
        }
    }
}
