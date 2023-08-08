using System;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;

namespace Monai.Deploy.InformaticsGateway.ExecutionPlugins
{
    public class ExternalAppOutgoing : IOutputDataPlugin
    {
        private readonly ILogger<ExternalAppOutgoing> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ExternalAppOutgoing(
            ILogger<ExternalAppOutgoing> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            //these are the standard tags, but this needs moving into config.
            DicomTag[] tags = { DicomTag.StudyInstanceUID, DicomTag.AccessionNumber, DicomTag.SeriesInstanceUID, DicomTag.SOPInstanceUID };

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var remoteAppExecution = await GetRemoteAppExecution(exportRequestDataMessage, tags).ConfigureAwait(false);
            remoteAppExecution.StudyUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);

            await repository.AddAsync(remoteAppExecution).ConfigureAwait(false);
            _logger.LogStudyUidChanged(remoteAppExecution.StudyUid, remoteAppExecution.OutgoingStudyUid);

            foreach (var tag in tags)
            {
                if (tag.Equals(DicomTag.StudyInstanceUID) is false)
                {
                    remoteAppExecution.OriginalValues.Add(tag, dicomFile.Dataset.GetString(tag));
                    dicomFile.Dataset.AddOrUpdate(tag, DicomUIDGenerator.GenerateDerivedFromUUID());
                }
            }

            dicomFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, remoteAppExecution.OutgoingStudyUid);

            return (dicomFile, exportRequestDataMessage);
        }

        private async Task<RemoteAppExecution> GetRemoteAppExecution(ExportRequestDataMessage request, DicomTag[] tags)
        {
            var remoteAppExecution = new RemoteAppExecution
            {
                CorrelationId = request.CorrelationId,
                WorkflowInstanceId = request.WorkflowInstanceId,
                ExportTaskId = request.ExportTaskId,
                Files = new System.Collections.Generic.List<string> { request.Filename },
                Status = request.ExportStatus
            };


            var outgoingStudyUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            remoteAppExecution.OutgoingStudyUid = outgoingStudyUid;


            foreach (var destination in request.Destinations)
            {
                remoteAppExecution.ExportDetails.Add(await LookupDestinationAsync(destination, new CancellationToken()));
            }

            return remoteAppExecution;
        }

        private async Task<DestinationApplicationEntity> LookupDestinationAsync(string destinationName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destinationName))
            {
                throw new ConfigurationException("Export task does not have destination set.");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDestinationApplicationEntityRepository>();
            var destination = await repository.FindByNameAsync(destinationName, cancellationToken).ConfigureAwait(false);

            if (destination is null)
            {
                throw new ConfigurationException($"Specified destination '{destinationName}' does not exist.");
            }

            return destination;
        }
    }
}
