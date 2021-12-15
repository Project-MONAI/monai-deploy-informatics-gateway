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

using Moq;
using Moq.Protected;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Client.Test
{
    public class ServiceTestBase
    {
        protected static HttpClient SetupHttpClientMock(string baseUrl, string expectedUrl, HttpMethod httpMethod, HttpResponseMessage httpResponse)
        {
            expectedUrl = Uri.EscapeDataString(expectedUrl);
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == httpMethod),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            return new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri(baseUrl)
            };
        }
    }
}
