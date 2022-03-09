// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
        }

        /// <inheritdoc/>
        public void ConfigureServiceUris(Uri uriRoot)
        {
            Guard.Against.MalformUri(uriRoot, nameof(uriRoot));

            _httpClient.BaseAddress = uriRoot;
        }

        /// <inheritdoc/>
        public void ConfigureAuthentication(AuthenticationHeaderValue value)
        {
            Guard.Against.Null(value, nameof(value));

            _httpClient.DefaultRequestHeaders.Authorization = value;
        }
    }
}
