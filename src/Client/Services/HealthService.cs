// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;

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

        public async Task<string> Live(CancellationToken cancellationToken) => await LiveReady("live", cancellationToken).ConfigureAwait(false);

        public async Task<string> Ready(CancellationToken cancellationToken) => await LiveReady("ready", cancellationToken).ConfigureAwait(false);

        public async Task<HealthStatusResponse> Status(CancellationToken cancellationToken)
        {
            Logger.SendingRequestTo($"{Route}/status");
            var response = await HttpClient.GetAsync($"{Route}/status", cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<HealthStatusResponse>(cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> LiveReady(string uriPath, CancellationToken cancellationToken)
        {
            Logger.SendingRequestTo($"{Route}/{uriPath}");
            var response = await HttpClient.GetAsync($"{Route}/{uriPath}", cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
