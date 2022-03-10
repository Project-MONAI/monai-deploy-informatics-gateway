﻿// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    public interface IHealthService
    {
        Task<HealthStatusResponse> Status(CancellationToken cancellationToken);

        Task<string> Ready(CancellationToken cancellationToken);

        Task<string> Live(CancellationToken cancellationToken);
    }

    internal class HealthService : ServiceBase, IHealthService
    {
        private static readonly string Route = "health";

        public HealthService(HttpClient httpClient, ILogger logger = null)
            : base(httpClient, logger)
        {
            Guard.Against.Null(httpClient, nameof(httpClient));
        }

        public async Task<string> Live(CancellationToken cancellationToken) => await LiveReady("live", cancellationToken);

        public async Task<string> Ready(CancellationToken cancellationToken) => await LiveReady("ready", cancellationToken);

        public async Task<HealthStatusResponse> Status(CancellationToken cancellationToken)
        {
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}/status");
            try
            {
                var response = await HttpClient.GetAsync($"{Route}/status", cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
                return await response.Content.ReadAsAsync<HealthStatusResponse>(cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        private async Task<string> LiveReady(string uriPath, CancellationToken cancellationToken)
        {
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}/{uriPath}");
            try
            {
                var response = await HttpClient.GetAsync($"{Route}/{uriPath}", cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                throw;
            }
        }
    }
}
