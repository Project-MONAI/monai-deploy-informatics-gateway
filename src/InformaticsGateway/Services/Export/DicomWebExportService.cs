/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.Messaging.Events;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class DicomWebExportService : ExportServiceBase
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DicomWebExportService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IDicomToolkit _dicomToolkit;

        protected override ushort Concurrency { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "DICOMweb Export Service";

        public DicomWebExportService(
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DicomWebExportService> logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));

            RoutingKey = $"{configuration.Value.Messaging.Topics.ExportRequestPrefix}.{configuration.Value.DicomWeb.AgentName}";
            Concurrency = configuration.Value.DicomWeb.MaximumNumberOfConnection;
        }

        protected override async Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId }, { "Filename", exportRequestData.Filename } });

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInferenceRequestRepository>();

            foreach (var transaction in exportRequestData.Destinations)
            {
                await HandleTransaction(exportRequestData, repository, transaction, cancellationToken).ConfigureAwait(false);
            }

            return exportRequestData;
        }

        private async Task HandleTransaction(ExportRequestDataMessage exportRequestData, IInferenceRequestRepository repository, string transaction, CancellationToken cancellationToken)
        {
            Guard.Against.Null(exportRequestData, nameof(exportRequestData));
            Guard.Against.Null(repository, nameof(repository));
            Guard.Against.NullOrWhiteSpace(transaction, nameof(transaction));

            var inferenceRequest = await repository.GetInferenceRequestAsync(transaction, cancellationToken).ConfigureAwait(false);
            if (inferenceRequest is null)
            {
                var errorMessage = $"The specified inference request '{transaction}' cannot be found and will not be exported.";
                _logger.InferenceRequestExportDestinationNotFound(transaction);
                exportRequestData.SetFailed(FileExportStatus.ConfigurationError, errorMessage);
                return;
            }

            var destinations = inferenceRequest.OutputResources.Where(p => p.Interface == InputInterfaceType.DicomWeb);

            if (!destinations.Any())
            {
                var errorMessage = "The inference request '{transaction}' contains no `outputResources` nor any DICOMweb export destinations.";
                _logger.InferenceRequestExportNoDestinationNotFound();
                exportRequestData.SetFailed(FileExportStatus.ConfigurationError, errorMessage);
                return;
            }

            foreach (var destination in destinations)
            {
                var authenticationHeader = AuthenticationHeaderValueExtensions.ConvertFrom(destination.ConnectionDetails.AuthType, destination.ConnectionDetails.AuthId);
                var dicomWebClient = new DicomWebClient(_httpClientFactory.CreateClient("dicomweb"), _loggerFactory.CreateLogger<DicomWebClient>());
                dicomWebClient.ConfigureServiceUris(new Uri(destination.ConnectionDetails.Uri, UriKind.Absolute));
                dicomWebClient.ConfigureAuthentication(authenticationHeader);

                _logger.ExportToDicomWeb(destination.ConnectionDetails.Uri);
                await ExportToDicomWebDestination(dicomWebClient, exportRequestData, destination, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ExportToDicomWebDestination(IDicomWebClient dicomWebClient, ExportRequestDataMessage exportRequestData, RequestOutputDataResource destination, CancellationToken cancellationToken)
        {
            DicomFile dicomFile;
            try
            {
                dicomFile = _dicomToolkit.Load(exportRequestData.FileContent);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error reading DICOM file: {ex.Message}.";
                _logger.ExportException(errorMessage, ex);
                exportRequestData.SetFailed(FileExportStatus.UnsupportedDataType, errorMessage);
                return;
            }

            try
            {
                await Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(
                       _configuration.Value.Export.Retries.RetryDelays,
                       (exception, timeSpan, retryCount, context) =>
                       {
                           _logger.ErrorExportingDicomWebWithRetry(destination.ConnectionDetails.Uri, timeSpan, retryCount, exception);
                       })
                   .ExecuteAsync(async () =>
                       {
                           var result = await dicomWebClient.Stow.Store(new List<DicomFile> { dicomFile }, cancellationToken).ConfigureAwait(false);
                           CheckAndLogResult(result);
                       }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                _logger.ExportException(errorMessage, ex);
                exportRequestData.SetFailed(FileExportStatus.ServiceError, errorMessage);
            }
        }

        private void CheckAndLogResult(DicomWebResponse<string> result)
        {
            Guard.Against.Null(result, nameof(result));
            switch (result.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    _logger.ExportSuccessfully();
                    break;

                default:
                    throw new ServiceException("Failed to export to destination.");
            }
        }
    }
}
