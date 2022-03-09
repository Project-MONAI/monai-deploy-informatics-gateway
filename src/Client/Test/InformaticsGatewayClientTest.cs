// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Moq;
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
