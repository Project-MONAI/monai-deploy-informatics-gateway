/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Net.Http;
using System.Net.Http.Headers;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Monai.Deploy.InformaticsGateway.Client.Services;

namespace Monai.Deploy.InformaticsGateway.Client
{
    public class InformaticsGatewayClient : IInformaticsGatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InformaticsGatewayClient> _logger;

        /// <inheritdoc/>
        public IHealthService Health { get; }

        /// <inheritdoc/>
        public IInferenceService Inference { get; }

        /// <inheritdoc/>
        public IAeTitleService<MonaiApplicationEntity> MonaiScpAeTitle { get; }

        /// <inheritdoc/>
        public IAeTitleService<SourceApplicationEntity> DicomSources { get; }

        /// <inheritdoc/>
        public IAeTitleService<DestinationApplicationEntity> DicomDestinations { get; }

        /// <inheritdoc/>
        public IAeTitleService<VirtualApplicationEntity> VirtualAeTitle { get; }

        /// <summary>
        /// Initializes a new instance of the InformaticsGatewayClient class that connects to the specified URI using the credentials provided.
        /// </summary>
        /// <param name="httpClient">HTTP client .</param>
        /// <param name="logger">Optional logger for capturing client logs.</param>
        public InformaticsGatewayClient(HttpClient httpClient, ILogger<InformaticsGatewayClient> logger)
        {
            Guard.Against.Null(httpClient, nameof(httpClient));

            _httpClient = httpClient;
            _logger = logger;

            Health = new HealthService(_httpClient, _logger);
            Inference = new InferenceService(_httpClient, _logger);
            MonaiScpAeTitle = new AeTitleService<MonaiApplicationEntity>("config/ae", _httpClient, _logger);
            DicomSources = new AeTitleService<SourceApplicationEntity>("config/source", _httpClient, _logger);
            DicomDestinations = new AeTitleService<DestinationApplicationEntity>("config/destination", _httpClient, _logger);
            VirtualAeTitle = new AeTitleService<VirtualApplicationEntity>("config/vae", _httpClient, _logger);
        }

        /// <inheritdoc/>
        public void ConfigureServiceUris(Uri uriRoot)
        {
            Guard.Against.MalformUri(uriRoot, nameof(uriRoot));

            _httpClient.BaseAddress = uriRoot;
            _logger?.BaseAddressSet(uriRoot);
        }

        /// <inheritdoc/>
        public void ConfigureAuthentication(AuthenticationHeaderValue value)
        {
            Guard.Against.Null(value, nameof(value));

            _httpClient.DefaultRequestHeaders.Authorization = value;
        }
    }
}
