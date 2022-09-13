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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Monai.Deploy.InformaticsGateway.Repositories;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    public class MonaiHealthCheck : IHealthCheck
    {
        private readonly IMonaiServiceLocator _monaiServiceLocator;

        public MonaiHealthCheck(IMonaiServiceLocator monaiServiceLocator)
        {
            _monaiServiceLocator = monaiServiceLocator ?? throw new ArgumentNullException(nameof(monaiServiceLocator));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var services = _monaiServiceLocator.GetServiceStatus();

            if (services.Values.All(p => p == Api.Rest.ServiceStatus.Running))
            {
                return Task.FromResult(HealthCheckResult.Healthy());
            }

            if (services.Values.All(p => p == Api.Rest.ServiceStatus.Stopped ||
                                            p == Api.Rest.ServiceStatus.Cancelled ||
                                            p == Api.Rest.ServiceStatus.Unknown))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy());
            }

            var sb = new StringBuilder();
            foreach (var service in services.Keys)
            {
                if (services[service] != Api.Rest.ServiceStatus.Running)
                {
                    sb.AppendLine($"{service}: {services[service]}");
                }
            }
            return Task.FromResult(HealthCheckResult.Degraded(sb.ToString()));
        }
    }
}
