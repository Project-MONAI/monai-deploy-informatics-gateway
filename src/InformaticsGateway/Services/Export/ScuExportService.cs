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
using Dicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DicomClient = Dicom.Network.Client.DicomClient;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    internal class ScuExportService : ExportServiceBase
    {
        private readonly ILogger<ScuExportService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ScuConfiguration _scuConfiguration;
        private readonly IDicomToolkit _dicomToolkit;

        protected override string Agent { get; }
        protected override int Concurrentcy { get; }

        public ScuExportService(
            ILogger<ScuExportService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IStorageInfoProvider storageInfoProvider,
            IDicomToolkit dicomToolkit)
            : base(logger, configuration, serviceScopeFactory, storageInfoProvider)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (dicomToolkit is null)
            {
                throw new ArgumentNullException(nameof(dicomToolkit));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _scuConfiguration = configuration.Value.Dicom.Scu;
            _dicomToolkit = dicomToolkit;
            Agent = _scuConfiguration.AeTitle;
            Concurrentcy = _scuConfiguration.MaximumNumberOfAssociations;
        }

        private DestinationApplicationEntity LookupDestination(OutputJob outputJob)
        {
            Guard.Against.Null(outputJob, nameof(outputJob));

            if (string.IsNullOrEmpty(outputJob.Parameters))
                throw new ConfigurationException("Task Parameter is missing destination");

            var dest = JsonConvert.DeserializeObject<string>(outputJob.Parameters);

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<DestinationApplicationEntity>>();
            var destination = repository.FirstOrDefault(p => p.Name.Equals(dest, StringComparison.InvariantCultureIgnoreCase));

            if (destination is null)
            {
                throw new ConfigurationException($"Specified destination '{dest}' does not exist");
            }

            return destination;
        }

        protected override async Task<OutputJob> ExportDataBlockCallback(OutputJob outputJob, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new LogginDataDictionary<string, object> { { "ExportTaskId", outputJob.ExportTaskId }, { "CorrelationId", outputJob.CorrelationId } });

            var manualResetEvent = new ManualResetEvent(false);
            DicomClient client = null;
            try
            {
                var destination = LookupDestination(outputJob);
                client = new DicomClient(
                    destination.HostIp,
                    destination.Port,
                    false,
                    _scuConfiguration.AeTitle,
                    destination.AeTitle);

                client.AssociationAccepted += (sender, args) => _logger.Log(LogLevel.Information, "Association accepted.");
                client.AssociationRejected += (sender, args) => _logger.Log(LogLevel.Warning, "Association rejected.");
                client.AssociationReleased += (sender, args) => _logger.Log(LogLevel.Information, "Association release.");

                client.Options = new DicomServiceOptions
                {
                    LogDataPDUs = _scuConfiguration.LogDataPdus,
                    LogDimseDatasets = _scuConfiguration.LogDimseDatasets
                };
                client.NegotiateAsyncOps();
                GenerateRequests(outputJob, client, manualResetEvent);
                _logger.Log(LogLevel.Information, "Sending job to {0}@{1}:{2}", destination.AeTitle, destination.HostIp, destination.Port);
                await client.SendAsync(cancellationToken).ConfigureAwait(false);

                manualResetEvent.WaitOne();
                _logger.LogInformation("Job sent to {0} completed", destination.AeTitle);
            }
            catch (Exception ex)
            {
                HandleCStoreException(ex, outputJob, client);
                outputJob.State = State.Failed;
            }

            return outputJob;
        }

        private void GenerateRequests(
            OutputJob job,
            DicomClient client,
            ManualResetEvent manualResetEvent)
        {
            try
            {
                var dicomFile = _dicomToolkit.Load(job.FileContent);

                var request = new DicomCStoreRequest(dicomFile);

                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        _logger.Log(LogLevel.Information, "Job {0} sent successfully", job.ExportTaskId);
                        job.State = State.Succeeded;
                    }
                    else
                    {
                        _logger.Log(LogLevel.Error, $"Failed to export job {job.ExportTaskId} with error {response.Status}");
                        job.State = State.Failed;
                    }
                    manualResetEvent.Set();
                };

                client.AddRequestAsync(request).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError("Error while adding DICOM C-STORE request: {0}", exception);
            }
        }

        private void HandleCStoreException(Exception ex, OutputJob job, DicomClient client)
        {
            var exception = ex;

            if (exception is AggregateException)
            {
                exception = exception.InnerException;
            }

            if (exception is DicomAssociationAbortedException abortEx)
            {
                _logger.Log(LogLevel.Error, abortEx, "Association aborted with reason {0}.", abortEx.AbortReason);
            }
            else if (exception is DicomAssociationRejectedException rejectEx)
            {
                _logger.Log(LogLevel.Error, rejectEx, "Association rejected with reason {0}.", rejectEx.RejectReason);
            }
            else if (exception is IOException && exception?.InnerException is SocketException socketException)
            {
                _logger.Log(LogLevel.Error, socketException, "Association aborted with error {0}.", socketException.Message);
            }
            else
            {
                _logger.Log(LogLevel.Error, ex, "Job failed with error: {0}.", exception.Message);
            }
        }
    }
}