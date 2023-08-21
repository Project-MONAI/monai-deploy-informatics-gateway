/*
 * Copyright 2023 MONAI Consortium
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

using System.Reflection;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution
{
    [PlugInName("Remote App Execution Incoming")]
    public class DicomReidentifier : IInputDataPlugIn
    {
        private readonly ILogger<DicomReidentifier> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public string Name => GetType().GetCustomAttribute<PlugInNameAttribute>()?.Name ?? GetType().Name;

        public DicomReidentifier(
            ILogger<DicomReidentifier> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        public async Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> ExecuteAsync(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var sopInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
            var remoteAppExecution = await repository.GetAsync(sopInstanceUid).ConfigureAwait(false);

            if (remoteAppExecution is null)
            {
                _logger.IncomingInstanceNotFound(sopInstanceUid);
                return (dicomFile, fileMetadata);
            }

            foreach (var key in remoteAppExecution.OriginalValues.Keys)
            {
                dicomFile.Dataset.AddOrUpdate(DicomTag.Parse(key), remoteAppExecution.OriginalValues[key]);
            }

            fileMetadata.WorkflowInstanceId = remoteAppExecution.WorkflowInstanceId;
            fileMetadata.TaskId = remoteAppExecution.ExportTaskId;
            fileMetadata.ChangeCorrelationId(_logger, remoteAppExecution.CorrelationId);

            return (dicomFile, fileMetadata);
        }
    }
}
