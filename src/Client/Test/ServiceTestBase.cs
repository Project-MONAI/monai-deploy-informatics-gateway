// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace Monai.Deploy.InformaticsGateway.Client.Test
{
    public class ServiceTestBase
    {
        protected ServiceTestBase()
        {
        }

        protected static HttpClient SetupHttpClientMock(Uri baseUri, HttpMethod httpMethod, HttpResponseMessage httpResponse)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == httpMethod),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            return new HttpClient(mockHandler.Object)
            {
                BaseAddress = baseUri
            };
        }
    }
}
