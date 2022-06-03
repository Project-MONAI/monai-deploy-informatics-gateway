// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    public interface IAeTitleService<T>
    {
        Task<IReadOnlyList<T>> List(CancellationToken cancellationToken);

        Task<T> GetAeTitle(string aeTitle, CancellationToken cancellationToken);

        Task<T> Create(T item, CancellationToken cancellationToken);

        Task<T> Delete(string aeTitle, CancellationToken cancellationToken);
    }

    internal class AeTitleService<T> : ServiceBase, IAeTitleService<T>
    {
        private string Route { get; }

        public AeTitleService(string route, HttpClient httpClient, ILogger logger = null)
            : base(httpClient, logger)
        {
            Guard.Against.NullOrWhiteSpace(route, nameof(route));
            Guard.Against.Null(httpClient, nameof(httpClient));

            Route = route;
        }

        public async Task<T> Create(T item, CancellationToken cancellationToken)
        {
            Guard.Against.Null(item, nameof(item));

            Logger.SendingRequestTo(Route);
            var response = await HttpClient.PostAsJsonAsync(Route, item, Configuration.JsonSerializationOptions, cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<T>(cancellationToken).ConfigureAwait(false);
        }

        public async Task<T> Delete(string aeTitle, CancellationToken cancellationToken)
        {
            aeTitle = Uri.EscapeDataString(aeTitle);
            Guard.Against.NullOrWhiteSpace(aeTitle, nameof(aeTitle));
            Logger.SendingRequestTo($"{Route}/{aeTitle}");
            var response = await HttpClient.DeleteAsync($"{Route}/{aeTitle}", cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T>(Configuration.JsonSerializationOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task<T> GetAeTitle(string aeTitle, CancellationToken cancellationToken)
        {
            aeTitle = Uri.EscapeDataString(aeTitle);
            Guard.Against.NullOrWhiteSpace(aeTitle, nameof(aeTitle));
            Logger.SendingRequestTo($"{Route}/{aeTitle}");
            var response = await HttpClient.GetAsync($"{Route}/{aeTitle}", cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T>(Configuration.JsonSerializationOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<T>> List(CancellationToken cancellationToken)
        {
            Logger.SendingRequestTo(Route);
            var response = await HttpClient.GetAsync(Route, cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            var list = await response.Content.ReadFromJsonAsync<IEnumerable<T>>(Configuration.JsonSerializationOptions, cancellationToken).ConfigureAwait(false);
            return list.ToList().AsReadOnly();
        }
    }
}
