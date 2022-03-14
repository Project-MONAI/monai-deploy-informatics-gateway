// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    public interface IAeTitleService<T>
    {
        Task<IReadOnlyList<T>> List(CancellationToken cancellationToken);

        Task<T> Get(string aeTitle, CancellationToken cancellationToken);

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

            Logger.Log(LogLevel.Debug, $"Sending request to {Route}");
            var response = await HttpClient.PostAsync<T>(Route, item, new JsonMediaTypeFormatter(), cancellationToken);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
            return await response.Content.ReadAsAsync<T>(cancellationToken);
        }

        public async Task<T> Delete(string aeTitle, CancellationToken cancellationToken)
        {
            aeTitle = Uri.EscapeUriString(aeTitle);
            Guard.Against.NullOrWhiteSpace(aeTitle, nameof(aeTitle));
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}/{aeTitle}");
            var response = await HttpClient.DeleteAsync($"{Route}/{aeTitle}", cancellationToken);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
            return await response.Content.ReadAsAsync<T>(cancellationToken);
        }

        public async Task<T> Get(string aeTitle, CancellationToken cancellationToken)
        {
            aeTitle = Uri.EscapeUriString(aeTitle);
            Guard.Against.NullOrWhiteSpace(aeTitle, nameof(aeTitle));
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}/{aeTitle}");
            var response = await HttpClient.GetAsync($"{Route}/{aeTitle}", cancellationToken);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
            return await response.Content.ReadAsAsync<T>(cancellationToken);
        }

        public async Task<IReadOnlyList<T>> List(CancellationToken cancellationToken)
        {
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}");
            var response = await HttpClient.GetAsync(Route, cancellationToken);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
            var list = await response.Content.ReadAsAsync<IEnumerable<T>>(cancellationToken);
            return list.ToList().AsReadOnly();
        }
    }
}
