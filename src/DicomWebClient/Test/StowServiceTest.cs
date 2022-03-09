// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Moq;
using Moq.Protected;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class StowServiceTest : IClassFixture<DicomFileGeneratorFixture>
    {
        private const string BaseUri = "http://dummy/api/";
        private readonly DicomFileGeneratorFixture _fixture;
        private readonly Mock<ILogger> _logger;

        public StowServiceTest(DicomFileGeneratorFixture fixture)
        {
            _fixture = fixture;
            _logger = new Mock<ILogger>();
        }

        [Fact(DisplayName = "Store - throws if input is null or empty")]
        public async Task Store_ShallThrowIfNoFilesSpecified()
        {
            var httpClient = new HttpClient();
            var service = new StowService(httpClient, _logger.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.Store(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await service.Store(new List<DicomFile>()));
        }

        [Fact(DisplayName = "Store - throws if no files match study instance UID")]
        public async Task Store_ShallThrowIfNoFilesMatchStudyInstanceUid()
        {
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID();
            var instances = _fixture.GenerateDicomFiles(3, studyInstanceUid);

            var httpClient = new HttpClient();
            var service = new StowService(httpClient, _logger.Object);

            var otherStudyInstanceUid = "1.2.3.4.5";
            await Assert.ThrowsAsync<ArgumentException>(async () => await service.Store(otherStudyInstanceUid, instances));
        }

        [Fact(DisplayName = "Store - handles SendAsync failures")]
        public async Task Store_HandlesSendAsyncFailures()
        {
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID();
            var instances = _fixture.GenerateDicomFiles(1, studyInstanceUid);

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
                .Throws(new Exception("unknown"));
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(BaseUri)
            };

            var service = new StowService(httpClient, _logger.Object);

            var exception = await Assert.ThrowsAsync<DicomWebClientException>(async () => await service.Store(instances));

            Assert.Null(exception.StatusCode);
        }

        [Theory(DisplayName = "Store - handles responses")]
        [InlineData(HttpStatusCode.OK, "response content")]
        [InlineData(HttpStatusCode.Conflict, "error content")]
        public async Task Store_HandlesResponses(HttpStatusCode status, string message)
        {
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID();
            var instances = _fixture.GenerateDicomFiles(3, studyInstanceUid);

            var response = new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(message)
            };

            GenerateHttpClient(response, out var handlerMock, out var httpClient);

            var service = new StowService(httpClient, _logger.Object);

            var dicomWebResponse = await service.Store(instances);

            Assert.IsType<DicomWebResponse<string>>(dicomWebResponse);
            Assert.Equal(message, dicomWebResponse.Result);
            Assert.Equal(status, dicomWebResponse.StatusCode);

            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.Content is MultipartContent &&
                req.RequestUri.ToString().Equals($"{BaseUri}studies/")),
               ItExpr.IsAny<CancellationToken>());
        }

        private static void GenerateHttpClient(HttpResponseMessage response, out Mock<HttpMessageHandler> handlerMock, out HttpClient httpClient)
        {
            handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(BaseUri)
            };
        }
    }
}
