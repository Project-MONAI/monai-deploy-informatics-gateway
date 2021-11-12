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

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class DicomWebExportService : ExportServiceBase
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DicomWebExportService> _logger;
        private readonly IDicomToolkit _dicomToolkit;

        protected override string Agent { get; }
        protected override int Concurrentcy { get; }
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
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Agent = configuration.Value.DicomWeb.ExportSink;
            Concurrentcy = configuration.Value.Dicom.Scu.MaximumNumberOfAssociations;
        }

        protected override async Task<OutputJob> ExportDataBlockCallback(OutputJob outputJob, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", outputJob.ExportTaskId }, { "CorrelationId", outputJob.CorrelationId } });

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInferenceRequestRepository>();
            var inferenceRequest = repository.Get(outputJob.Parameters); //TODO: make sure the transaction ID is stored here
            if (inferenceRequest is null)
            {
                _logger.Log(LogLevel.Error, "The specified job cannot be found in the inference request store and will not be exported.");
                await ReportFailure(outputJob, cancellationToken);
                return null;
            }

            var destinations = inferenceRequest.OutputResources.Where(p => p.Interface == InputInterfaceType.DicomWeb);

            if (destinations.Count() == 0)
            {
                _logger.Log(LogLevel.Error, "The inference request contains no `outputResources` nor any DICOMweb export destinations.");
                await ReportFailure(outputJob, cancellationToken);
                return null;
            }

            foreach (var destination in destinations)
            {
                var authenticationHeader = AuthenticationHeaderValueExtensions.ConvertFrom(destination.ConnectionDetails.AuthType, destination.ConnectionDetails.AuthId);
                var dicomWebClient = new DicomWebClient(_httpClientFactory.CreateClient("dicomweb"), _loggerFactory.CreateLogger<DicomWebClient>());
                dicomWebClient.ConfigureServiceUris(new Uri(destination.ConnectionDetails.Uri, UriKind.Absolute));
                dicomWebClient.ConfigureAuthentication(authenticationHeader);

                _logger.Log(LogLevel.Debug, $"Exporting data to {destination.ConnectionDetails.Uri}.");
                await ExportToDicomWebDestination(dicomWebClient, outputJob, destination, cancellationToken);
            }

            return outputJob;
        }

        private async Task ExportToDicomWebDestination(IDicomWebClient dicomWebClient, OutputJob outputJob, RequestOutputDataResource destination, CancellationToken cancellationToken)
        {
            try
            {
                var dicomFile = _dicomToolkit.Load(outputJob.FileContent);
                var result = await dicomWebClient.Stow.Store(new List<DicomFile> { dicomFile }, cancellationToken);
                CheckAndLogResult(result);
                outputJob.State = State.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to export data to DICOMweb destination.");
                outputJob.State = State.Failed;
            }
        }

        private void CheckAndLogResult(DicomWebResponse<string> result)
        {
            Guard.Against.Null(result, nameof(result));
            switch (result.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    _logger.Log(LogLevel.Information, "All data exported successfully.");
                    break;

                default:
                    throw new Exception("Failed to export to destination.");
            }
        }
    }
}
