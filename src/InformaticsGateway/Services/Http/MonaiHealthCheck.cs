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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    public class MonaiHealthCheck : IHealthCheck
    {
        private readonly IMonaiServiceLocator _monaiServiceLocator;
        private readonly ILogger<MonaiHealthCheck> _logger;

        public MonaiHealthCheck(IMonaiServiceLocator monaiServiceLocator, ILogger<MonaiHealthCheck> logger)
        {
            _monaiServiceLocator = monaiServiceLocator ?? throw new ArgumentNullException(nameof(monaiServiceLocator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var services = _monaiServiceLocator.GetServiceStatus();

            if (services.Values.All(p => p == Api.Rest.ServiceStatus.Running))
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            var unhealthyServices = services.Where(item => item.Value != Api.Rest.ServiceStatus.Running).ToDictionary(k => k.Key, v => (object)v.Value);

            if (unhealthyServices.Count == services.Count)
            {
                _logger.AllServiceUnheathly();
                return Task.FromResult(HealthCheckResult.Unhealthy(data: unhealthyServices));
            }
            _logger.SomeServiceUnheathly(string.Join(",", unhealthyServices.Keys));
            return Task.FromResult(HealthCheckResult.Degraded(data: unhealthyServices));
        }
    }
}
