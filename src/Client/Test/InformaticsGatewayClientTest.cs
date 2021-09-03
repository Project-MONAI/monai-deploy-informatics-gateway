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
using Moq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Client.Test
{
    public class InformaticsGatewayClientTest
    {
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<InformaticsGatewayClient>> _logger;

        public InformaticsGatewayClientTest()
        {
            _httpClient = new HttpClient();
            _logger = new Mock<ILogger<InformaticsGatewayClient>>();
        }

        [Fact(DisplayName = "Contructor configures all services")]
        public void Contructor()
        {
            var baseUri = new Uri("http://localhost:5000");
            var client = new InformaticsGatewayClient(_httpClient, _logger.Object);

            Assert.NotNull(client.MonaiScpAeTitle);
            Assert.NotNull(client.Health);
            Assert.NotNull(client.Inference);
            Assert.NotNull(client.DicomSources);
            Assert.NotNull(client.DicomDestinations);
        }

        [Fact(DisplayName = "ConfigureServiceUris")]
        public void ConfigureServiceUris()
        {
            var baseUri = new Uri("http://localhost:5000");
            var client = new InformaticsGatewayClient(_httpClient, _logger.Object);
            client.ConfigureServiceUris(baseUri);

            Assert.Equal(baseUri, _httpClient.BaseAddress);
        }

        [Fact(DisplayName = "ConfigureAuthentication")]
        public void ConfigureAuthentication()
        {
            var client = new InformaticsGatewayClient(_httpClient, _logger.Object);
            client.ConfigureAuthentication(new AuthenticationHeaderValue("basic", "token"));

            Assert.Equal("basic", _httpClient.DefaultRequestHeaders.Authorization.Scheme);
        }
    }
}
