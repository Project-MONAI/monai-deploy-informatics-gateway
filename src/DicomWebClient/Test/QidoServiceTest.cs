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
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Moq;
using Moq.Protected;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.DicomWebClient.Test
{
    public class QidoServiceTest : IClassFixture<DicomFileGeneratorFixture>
    {
        private const string BaseUri = "http://dummy/api/";
        private readonly DicomFileGeneratorFixture _fixture;

        public QidoServiceTest(DicomFileGeneratorFixture fixture)
        {
            _fixture = fixture;
        }

        #region SearchForStudies

        [Fact(DisplayName = "SearchForStudies - all studies returns JSON string")]
        public async Task SearchForStudies_AllStudies()
        {
            var studyUid = DicomUIDGenerator.GenerateDerivedFromUUID();

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = DicomFileGeneratorFixture.GenerateInstancesAsJson(1, studyUid),
            };

            GenerateHttpClient(response, out var handlerMock, out var httpClient);

            var qido = new QidoService(httpClient);

            var count = 0;
            await foreach (var instance in qido.SearchForStudies<string>())
            {
                count++;
                Assert.IsType<string>(instance);
            }

            Assert.Equal(1, count);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{BaseUri}studies/")),
               ItExpr.IsAny<CancellationToken>());
        }

        [Fact(DisplayName = "SearchForStudies - queryParameters - returns JSON string")]
        public async Task SearchForStudies_WithQueryParameters()
        {
            var studyUid = DicomUIDGenerator.GenerateDerivedFromUUID();

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = DicomFileGeneratorFixture.GenerateInstancesAsJson(1, studyUid),
            };

            GenerateHttpClient(response, out var handlerMock, out var httpClient);

            var qido = new QidoService(httpClient);

            var queryParameters = new Dictionary<string, string>
            {
                { "11112222", "value" }
            };

            var count = 0;
            await foreach (var instance in qido.SearchForStudies<string>(queryParameters))
            {
                count++;
                Assert.IsType<string>(instance);
            }

            Assert.Equal(1, count);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{BaseUri}studies/") &&
                req.RequestUri.Query.Contains("11112222=value")),
               ItExpr.IsAny<CancellationToken>());
        }

        [Fact(DisplayName = "SearchForStudies - queryParameters, fields - returns JSON string")]
        public async Task SearchForStudies_WithQueryParametersAndFields()
        {
            var studyUid = DicomUIDGenerator.GenerateDerivedFromUUID();

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = DicomFileGeneratorFixture.GenerateInstancesAsJson(1, studyUid),
            };

            GenerateHttpClient(response, out var handlerMock, out var httpClient);

            var qido = new QidoService(httpClient);

            var queryParameters = new Dictionary<string, string>
            {
                { "11112222", "value" }
            };
            var fields = new List<string>
            {
                "1234"
            };

            var count = 0;
            await foreach (var instance in qido.SearchForStudies<string>(queryParameters, fields))
            {
                count++;
                Assert.IsType<string>(instance);
            }

            Assert.Equal(1, count);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{BaseUri}studies/") &&
                req.RequestUri.Query.Contains("includefield=1234") &&
                req.RequestUri.Query.Contains("11112222=value")),
               ItExpr.IsAny<CancellationToken>());
        }

        [Fact(DisplayName = "SearchForStudies - all arguments - returns JSON string")]
        public async Task SearchForStudies_AllArguments()
        {
            var studyUid = DicomUIDGenerator.GenerateDerivedFromUUID();

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = DicomFileGeneratorFixture.GenerateInstancesAsJson(1, studyUid),
            };

            GenerateHttpClient(response, out var handlerMock, out var httpClient);

            var qido = new QidoService(httpClient);

            var queryParameters = new Dictionary<string, string>
            {
                { "11112222", "value" }
            };
            var fields = new List<string>
            {
                "1234"
            };

            var count = 0;
            await foreach (var instance in qido.SearchForStudies<string>(queryParameters, fields, true, 1, 1))
            {
                count++;
                Assert.IsType<string>(instance);
            }

            Assert.Equal(1, count);
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().StartsWith($"{BaseUri}studies/") &&
                req.RequestUri.Query.Contains("includefield=1234") &&
                req.RequestUri.Query.Contains("fuzzymatching=true") &&
                req.RequestUri.Query.Contains("limit=1") &&
                req.RequestUri.Query.Contains("offset=1") &&
                req.RequestUri.Query.Contains("11112222=value")),
               ItExpr.IsAny<CancellationToken>());
        }

        #endregion SearchForStudies

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
