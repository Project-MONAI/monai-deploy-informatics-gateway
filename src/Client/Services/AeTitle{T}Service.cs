// Copyright 2021 MONAI Consortium
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;

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

            _logger.Log(LogLevel.Debug, $"Sending request to {Route}");
            try
            {
                var response = await _httpClient.PostAsync<T>(Route, item, new JsonMediaTypeFormatter(), cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(_logger);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        public async Task<T> Delete(string name, CancellationToken cancellationToken)
        {
            name = Uri.EscapeUriString(name);
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            _logger.Log(LogLevel.Debug, $"Sending request to {Route}/{name}");
            try
            {
                var response = await _httpClient.DeleteAsync($"{Route}/{name}", cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(_logger);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        public async Task<T> Get(string name, CancellationToken cancellationToken)
        {
            name = Uri.EscapeUriString(name);
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            _logger.Log(LogLevel.Debug, $"Sending request to {Route}/{name}");
            try
            {
                var response = await _httpClient.GetAsync($"{Route}/{name}", cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(_logger);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        public async Task<IReadOnlyList<T>> List(CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, $"Sending request to {Route}");
            try
            {
                var response = await _httpClient.GetAsync(Route, cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(_logger);
                var list = await response.Content.ReadAsAsync<IEnumerable<T>>(cancellationToken);
                return list.ToList().AsReadOnly();
            }
            catch
            {
                throw;
            }
        }
    }
}
