// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
