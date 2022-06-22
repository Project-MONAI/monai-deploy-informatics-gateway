// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
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

        protected override int Concurrency { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "DICOMweb Export Service";

        public DicomWebExportService(
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DicomWebExportService> logger,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IStorageInfoProvider storageInfoProvider,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory, storageInfoProvider)
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
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId } });

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

            var inferenceRequest = repository.GetInferenceRequest(transaction);
            if (inferenceRequest is null)
            {
                var errorMessage = $"The specified inference request '{transaction}' cannot be found and will not be exported.";
                _logger.InferenceRequestExportDestinationNotFound(transaction);
                exportRequestData.SetFailed(errorMessage);
                return;
            }

            var destinations = inferenceRequest.OutputResources.Where(p => p.Interface == InputInterfaceType.DicomWeb);

            if (!destinations.Any())
            {
                var errorMessage = "The inference request '{transaction}' contains no `outputResources` nor any DICOMweb export destinations.";
                _logger.InferenceRequestExportNoDestinationNotFound();
                exportRequestData.SetFailed(errorMessage);
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
            try
            {
                var dicomFile = _dicomToolkit.Load(exportRequestData.FileContent);
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
                exportRequestData.SetFailed(errorMessage);
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
