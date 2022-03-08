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

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;

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
            try
            {
                var response = await HttpClient.PostAsync($"{Route}", request, new JsonMediaTypeFormatter(), cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
                return await response.Content.ReadAsAsync<InferenceRequestResponse>(cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        public async Task<InferenceStatusResponse> Status(string transactionId, CancellationToken cancellationToken)
        {
            Logger.Log(LogLevel.Debug, $"Sending request to {Route}/status");
            try
            {
                var response = await HttpClient.GetAsync($"{Route}/{transactionId}", cancellationToken);
                await response.EnsureSuccessStatusCodeWithProblemDetails(Logger);
                return await response.Content.ReadAsAsync<InferenceStatusResponse>(cancellationToken);
            }
            catch
            {
                throw;
            }
        }
    }
}
