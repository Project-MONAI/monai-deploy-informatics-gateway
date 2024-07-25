/*
 * Copyright 2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class MonaiHealthCheckTest
    {
        private readonly Mock<IMonaiServiceLocator> _monaiServiceLocator;
        private readonly ILogger<MonaiHealthCheck> _logger = new NullLogger<MonaiHealthCheck>();

        public MonaiHealthCheckTest()
        {
            _monaiServiceLocator = new Mock<IMonaiServiceLocator>();
        }

        [Fact]
        public async Task GivenAllServicesRunning_WhenCheckHealthAsyncIsCalled_ReturnsHealthy()
        {
            _monaiServiceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, Api.Rest.ServiceStatus>()
            {
                { "A", Api.Rest.ServiceStatus.Running },
                { "B", Api.Rest.ServiceStatus.Running },
                { "C", Api.Rest.ServiceStatus.Running },
            });

            var svc = new MonaiHealthCheck(_monaiServiceLocator.Object, _logger);
            var result = await svc.CheckHealthAsync(null);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task GivenSomeServicesNotRunning_WhenCheckHealthAsyncIsCalled_ReturnsDegraded()
        {
            _monaiServiceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, Api.Rest.ServiceStatus>()
            {
                { "A", Api.Rest.ServiceStatus.Running },
                { "B", Api.Rest.ServiceStatus.Cancelled },
                { "C", Api.Rest.ServiceStatus.Stopped },
            });

            var svc = new MonaiHealthCheck(_monaiServiceLocator.Object, _logger);
            var result = await svc.CheckHealthAsync(null);
            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Equal(Api.Rest.ServiceStatus.Cancelled, result.Data["B"]);
            Assert.Equal(Api.Rest.ServiceStatus.Stopped, result.Data["C"]);
        }

        [Fact]
        public async Task GivenAllServicesNotRunning_WhenCheckHealthAsyncIsCalled_ReturnsUnhealthy()
        {
            _monaiServiceLocator.Setup(p => p.GetServiceStatus()).Returns(new Dictionary<string, Api.Rest.ServiceStatus>()
            {
                { "A", Api.Rest.ServiceStatus.Stopped },
                { "B", Api.Rest.ServiceStatus.Cancelled },
                { "C", Api.Rest.ServiceStatus.Stopped },
            });

            var svc = new MonaiHealthCheck(_monaiServiceLocator.Object, _logger);
            var result = await svc.CheckHealthAsync(null);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal(Api.Rest.ServiceStatus.Stopped, result.Data["A"]);
            Assert.Equal(Api.Rest.ServiceStatus.Cancelled, result.Data["B"]);
            Assert.Equal(Api.Rest.ServiceStatus.Stopped, result.Data["C"]);
        }
    }
}
