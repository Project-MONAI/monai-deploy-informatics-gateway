using System;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;

namespace Monai.Deploy.InformaticsGateway.ExecutionPlugins
{
    public class ExternalAppIncoming : IInputDataPlugin
    {
        private readonly ILogger<ExternalAppIncoming> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ExternalAppIncoming(
            ILogger<ExternalAppIncoming> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> Execute(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var incommingStudyUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);
            var remoteAppExecution = await repository.GetAsync(incommingStudyUid);
            if (remoteAppExecution is null)
            {
                _logger.LogOriginalStudyUidNotFound(incommingStudyUid);
                return (dicomFile, fileMetadata);
            }
            foreach (var key in remoteAppExecution.OriginalValues.Keys)
            {
                dicomFile.Dataset.AddOrUpdate(key, remoteAppExecution.OriginalValues[key]);
            }
            dicomFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, remoteAppExecution.StudyUid);
            fileMetadata.WorkflowInstanceId = remoteAppExecution.WorkflowInstanceId;
            fileMetadata.TaskId = remoteAppExecution.ExportTaskId;

            return (dicomFile, fileMetadata);
        }
    }
}
