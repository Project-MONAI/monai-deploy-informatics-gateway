// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using BoDi;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client;
using Moq;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Hooks
{
    [Binding]
    public sealed class HttpClientHooks
    {
        private readonly IObjectContainer _objectContainer;
        private readonly HttpClient _client;
        private readonly Mock<ILogger<InformaticsGatewayClient>> _informaticsGatewayClientLogger;

        public HttpClientHooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
            _client = HttpClientFactory.Create();
            _informaticsGatewayClientLogger = new Mock<ILogger<InformaticsGatewayClient>>();
        }

        [BeforeScenario(Order = 1)]
        public void FirstBeforeScenario()
        {
            _objectContainer.RegisterInstanceAs(_client);
            _objectContainer.RegisterInstanceAs(_informaticsGatewayClientLogger.Object);
        }
    }
}
