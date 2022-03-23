// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Client.Services;
using Moq;
using Newtonsoft.Json;
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

            var json = JsonConvert.SerializeObject(problem);

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
            var json = JsonConvert.SerializeObject(inferenceStatus);

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

            var json = JsonConvert.SerializeObject(problem);

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
