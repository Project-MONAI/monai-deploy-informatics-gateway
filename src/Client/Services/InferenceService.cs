/*
 * Copyright 2021-2022 MONAI Consortium
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

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    public interface IInferenceService
    {
        Task<InferenceStatusResponse> Status(string transactionId, CancellationToken cancellationToken);

        Task<InferenceRequestResponse> NewInferenceRequest(InferenceRequest request, CancellationToken cancellationToken);
    }

    internal class InferenceService : ServiceBase, IInferenceService
    {
        private static readonly string Route = "inference";

        public InferenceService(HttpClient httpClient, ILogger logger = null)
            : base(httpClient, logger)
        {
        }

        public async Task<InferenceRequestResponse> NewInferenceRequest(InferenceRequest request, CancellationToken cancellationToken)
        {
            Logger.SendingRequestTo(Route);
            var response = await HttpClient.PostAsJsonAsync($"{Route}", request, Configuration.JsonSerializationOptions, cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadAsAsync<InferenceRequestResponse>(cancellationToken).ConfigureAwait(false);
        }

        public async Task<InferenceStatusResponse> Status(string transactionId, CancellationToken cancellationToken)
        {
            Logger.SendingRequestTo($"{Route}/status");
            var response = await HttpClient.GetAsync($"{Route}/{transactionId}", cancellationToken).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<InferenceStatusResponse>(Configuration.JsonSerializationOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
