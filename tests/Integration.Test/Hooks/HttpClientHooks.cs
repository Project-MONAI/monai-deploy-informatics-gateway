﻿// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        [AfterScenario]
        public void AfterScenario()
        {
        }
    }
}