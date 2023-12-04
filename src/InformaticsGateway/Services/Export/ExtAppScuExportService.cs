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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Models;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.Messaging.Common;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Services.Export
{
    public class ExtAppScuExportService : ExportServiceBase
    {
        private readonly ILogger<ExtAppScuExportService> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IExternalAppDetailsRepository _repository;
        private readonly IDicomToolkit _dicomToolkit;
        protected override ushort Concurrency { get; }
        public override string RoutingKey { get; }
        public override string ServiceName => "External App Export Service";

        public ExtAppScuExportService(
            ILogger<ExtAppScuExportService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IDicomToolkit dicomToolkit,
            IExternalAppDetailsRepository repository)
            : base(logger, configuration, serviceScopeFactory, dicomToolkit)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            RoutingKey = $"{configuration.Value.Messaging.Topics.ExternalAppRequest}";
            Concurrency = _configuration.Value.Dicom.Scu.MaximumNumberOfAssociations;
        }

        protected override async Task ProcessMessage(MessageReceivedEventArgs eventArgs)
        {
            var (exportFlow, reportingActionBlock) = SetupActionBlocks();

            lock (SyncRoot)
            {
                var externalAppRequest = eventArgs.Message.ConvertTo<ExternalAppRequestEvent>();
                if (ExportRequests.ContainsKey(externalAppRequest.ExportTaskId))
                {
                    _logger.ExportRequestAlreadyQueued(externalAppRequest.CorrelationId, externalAppRequest.ExportTaskId);
                    return;
                }

                externalAppRequest.MessageId = eventArgs.Message.MessageId;
                externalAppRequest.DeliveryTag = eventArgs.Message.DeliveryTag;

                var exportRequestWithDetails = new ExportRequestEventDetails(externalAppRequest);

                ExportRequests.Add(externalAppRequest.ExportTaskId, exportRequestWithDetails);
                if (!exportFlow.Post(exportRequestWithDetails))
                {
                    _logger.ErrorPostingExportJobToQueue(externalAppRequest.CorrelationId, externalAppRequest.ExportTaskId);
                    MessageSubscriber.Reject(eventArgs.Message);
                }
                else
                {
                    _logger.ExportRequestQueuedForProcessing(externalAppRequest.CorrelationId, externalAppRequest.MessageId, externalAppRequest.ExportTaskId);
                }
            }

            exportFlow.Complete();
            await reportingActionBlock.Completion.ConfigureAwait(false);
        }

        protected override async Task ExportCompleteCallback(ExportRequestDataMessage exportRequestData)
        {
            try
            {
                var dicom = _dicomToolkit.Load(exportRequestData.FileContent);
                dicom.Dataset.TryGetString(DicomTag.PatientID, out var patientID);
                if (dicom.Dataset.TryGetString(DicomTag.StudyInstanceUID, out var studyInstUID))
                {
                    var (newStudyInstanceUID, newPatientId) =
                        await SaveInRepo(exportRequestData, studyInstUID, patientID ?? string.Empty)
                        .ConfigureAwait(false);
                    dicom.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, newStudyInstanceUID);
                    dicom.Dataset.AddOrUpdate(DicomTag.PatientID, newPatientId);

                    using var ms = new MemoryStream();
                    await dicom.SaveAsync(ms).ConfigureAwait(false);
                    exportRequestData.SetData(ms.ToArray());
                    return;
                }
                throw new ExternalAppExeception("No StudyInstanceUID tag found");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error reading DICOM file: {ex.Message}";
                _logger.ExportException(errorMessage, ex);
                exportRequestData.SetFailed(FileExportStatus.UnsupportedDataType, errorMessage);
            }

        }

        private async Task<(string, string)> SaveInRepo(ExportRequestDataMessage externalAppRequest, string studyinstanceId, string patientId)
        {
            var existing = (await _repository.GetAsync(studyinstanceId, new CancellationToken()).ConfigureAwait(false))
                ?.Find(e => e.WorkflowInstanceId == externalAppRequest.WorkflowInstanceId &&
                            e.ExportTaskID == externalAppRequest.ExportTaskId);
            if (existing is null)
            {
                var studyInstanceUidOutBound = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
                var PatientIdOutbound = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
                await _repository.AddAsync(new ExternalAppDetails
                {
                    StudyInstanceUid = studyinstanceId,
                    StudyInstanceUidOutBound = studyInstanceUidOutBound,
                    WorkflowInstanceId = externalAppRequest.WorkflowInstanceId,
                    ExportTaskID = externalAppRequest.ExportTaskId,
                    CorrelationId = externalAppRequest.CorrelationId,
                    DateTimeCreated = DateTime.Now,
                    DestinationFolder = externalAppRequest.FilePayloadId,
                    PatientId = patientId,
                    PatientIdOutBound = PatientIdOutbound
                }, new CancellationToken()).ConfigureAwait(false);
                _logger.SavingExternalAppData(studyinstanceId);
                return (studyInstanceUidOutBound, PatientIdOutbound);
            }
            return (existing.StudyInstanceUidOutBound, existing.PatientIdOutBound);
        }

        protected override async Task<ExportRequestDataMessage> ExportDataBlockCallback(ExportRequestDataMessage exportRequestData, CancellationToken cancellationToken)
        {
            using var loggerScope = _logger.BeginScope(new Messaging.Common.LoggingDataDictionary<string, object> { { "ExportTaskId", exportRequestData.ExportTaskId }, { "CorrelationId", exportRequestData.CorrelationId }, { "Filename", exportRequestData.Filename } });

            foreach (var destinationName in exportRequestData.Destinations)
            {
                await HandleDesination(exportRequestData, destinationName, cancellationToken).ConfigureAwait(false);
            }

            return exportRequestData;
        }
    }
}
