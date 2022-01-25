// Copyright 2022 MONAI Consortium
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
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Polly;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class ScuExportService : ExportServiceBase
    {
        private readonly ILogger<ScuExportService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IDicomToolkit _dicomToolkit;

        protected override int Concurrentcy { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "DICOM Export Service";

        public ScuExportService(
            ILogger<ScuExportService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IStorageInfoProvider storageInfoProvider,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory, storageInfoProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));

            RoutingKey = $"{configuration.Value.Messaging.Topics.ExportRequestPrefix}.{_configuration.Value.Dicom.Scu.AgentName}";
            Concurrentcy = _configuration.Value.Dicom.Scu.MaximumNumberOfAssociations;
        }

        protected override async Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId } });

            var manualResetEvent = new ManualResetEvent(false);
            IDicomClient client = null;
            DestinationApplicationEntity destination = null;
            try
            {
                destination = LookupDestination(exportRequestData);
            }
            catch (ConfigurationException ex)
            {
                _logger.Log(LogLevel.Error, ex, ex.Message);
                exportRequestData.SetFailed(ex.Message);
                return exportRequestData;
            }

            try
            {
                client = DicomClientFactory.Create(
                    destination.HostIp,
                    destination.Port,
                    false,
                    _configuration.Value.Dicom.Scu.AeTitle,
                    destination.AeTitle);

                client.AssociationAccepted += (sender, args) => _logger.Log(LogLevel.Information, "Association accepted.");
                client.AssociationRejected += (sender, args) => _logger.Log(LogLevel.Warning, "Association rejected.");
                client.AssociationReleased += (sender, args) => _logger.Log(LogLevel.Information, "Association release.");
                client.ServiceOptions.LogDataPDUs = _configuration.Value.Dicom.Scu.LogDataPdus;
                client.ServiceOptions.LogDimseDatasets = _configuration.Value.Dicom.Scu.LogDimseDatasets;

                client.NegotiateAsyncOps();
                if (GenerateRequests(exportRequestData, client, manualResetEvent))
                {
                    await Policy
                       .Handle<Exception>()
                       .WaitAndRetryAsync(
                           _configuration.Value.Export.Retries.RetryDelays,
                           (exception, timeSpan, retryCount, context) =>
                           {
                               _logger.Log(LogLevel.Error, exception, $"Error exporting to DICOMweb destination. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
                           })
                       .ExecuteAsync(async () =>
                       {
                           _logger.Log(LogLevel.Information, "Sending job to {0}@{1}:{2}", destination.AeTitle, destination.HostIp, destination.Port);
                           await client.SendAsync(cancellationToken).ConfigureAwait(false);
                           manualResetEvent.WaitOne();
                           _logger.LogInformation("Job sent to {0} completed", destination.AeTitle);
                       });
                }
            }
            catch (Exception ex)
            {
                HandleCStoreException(ex, exportRequestData, client);
            }

            return exportRequestData;
        }

        private DestinationApplicationEntity LookupDestination(ExportRequestDataMessage exportRequestData)
        {
            Guard.Against.Null(exportRequestData, nameof(exportRequestData));

            if (string.IsNullOrWhiteSpace(exportRequestData.Destination))
            {
                throw new ConfigurationException("Export task does not have destination set.");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<DestinationApplicationEntity>>();
            var destination = repository.FirstOrDefault(p => p.Name.Equals(exportRequestData.Destination, StringComparison.InvariantCultureIgnoreCase));

            if (destination is null)
            {
                throw new ConfigurationException($"Specified destination '{exportRequestData.Destination}' does not exist");
            }

            return destination;
        }

        private bool GenerateRequests(
            ExportRequestDataMessage exportRequestData,
            IDicomClient client,
            ManualResetEvent manualResetEvent)
        {
            try
            {
                var dicomFile = _dicomToolkit.Load(exportRequestData.FileContent);

                var request = new DicomCStoreRequest(dicomFile);

                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        _logger.Log(LogLevel.Information, "Job sent successfully.");
                    }
                    else
                    {
                        var errorMessage = $"Failed to export with error {response.Status}";
                        _logger.Log(LogLevel.Error, errorMessage);
                        exportRequestData.SetFailed(errorMessage);
                    }
                    manualResetEvent.Set();
                };

                client.AddRequestAsync(request).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error while adding DICOM C-STORE request: {exception.Message}";
                _logger.Log(LogLevel.Error, exception, errorMessage);
                exportRequestData.SetFailed(errorMessage);
                return false;
            }
        }

        private void HandleCStoreException(Exception ex, ExportRequestDataMessage exportRequestData, IDicomClient client)
        {
            var exception = ex;

            if (exception is AggregateException)
            {
                exception = exception.InnerException;
            }

            var errorMessage = $"Job failed with error: {exception.Message}.";

            if (exception is DicomAssociationAbortedException abortEx)
            {
                errorMessage = $"Association aborted with reason {abortEx.AbortReason}.";
            }
            else if (exception is DicomAssociationRejectedException rejectEx)
            {
                errorMessage = $"Association rejected with reason {rejectEx.RejectReason}.";
            }
            else if (exception is SocketException socketException)
            {
                errorMessage = $"Association aborted with error {socketException.Message}.";
            }

            _logger.Log(LogLevel.Error, ex, errorMessage);
            exportRequestData.SetFailed(errorMessage);
        }
    }
}
