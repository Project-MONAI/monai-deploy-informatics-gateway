// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    public interface IInferenceService
    {
        Task<InferenceStatusResponse> Status(string transactionId, CancellationToken cancellationToken);

        Task<InferenceRequestResponse> New(InferenceRequest request, CancellationToken cancellationToken);
    }

    internal class InferenceService : ServiceBase, IInferenceService
    {
        private static readonly string Route = "inference";

        public InferenceService(HttpClient httpClient, ILogger logger = null)
            : base(httpClient, logger)
        {
        }

        public async Task<InferenceRequestResponse> New(InferenceRequest request, CancellationToken cancellationToken)
        {
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}");
            var response = await HttpClient.PostAsync($"{Route}", request, new JsonMediaTypeFormatter(), cancellationToken);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
            return await response.Content.ReadAsAsync<InferenceRequestResponse>(cancellationToken);
        }

        public async Task<InferenceStatusResponse> Status(string transactionId, CancellationToken cancellationToken)
        {
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}/status");
            var response = await HttpClient.GetAsync($"{Route}/{transactionId}", cancellationToken);
            await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
            return await response.Content.ReadAsAsync<InferenceStatusResponse>(cancellationToken);
        }
    }
}
