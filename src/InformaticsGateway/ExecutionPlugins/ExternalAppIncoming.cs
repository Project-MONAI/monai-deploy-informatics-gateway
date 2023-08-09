/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;

namespace Monai.Deploy.InformaticsGateway.ExecutionPlugins
{
    public class ExternalAppIncoming : IInputDataPlugin
    {
        private readonly ILogger<ExternalAppIncoming> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly PluginConfiguration _options;

        public ExternalAppIncoming(
            ILogger<ExternalAppIncoming> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<PluginConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _options = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
            if (_options.Configuration.ContainsKey("ReplaceTags") is false) { throw new ArgumentNullException(nameof(configuration)); }
        }

        public async Task<(DicomFile dicomFile, FileStorageMetadata fileMetadata)> Execute(DicomFile dicomFile, FileStorageMetadata fileMetadata)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var tagUsedAsKey = GetTags(_options.Configuration["ReplaceTags"]).First();

            var incommingStudyUid = dicomFile.Dataset.GetString(tagUsedAsKey);
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
            //dicomFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, remoteAppExecution.StudyUid);
            fileMetadata.WorkflowInstanceId = remoteAppExecution.WorkflowInstanceId;
            fileMetadata.TaskId = remoteAppExecution.ExportTaskId;

            return (dicomFile, fileMetadata);
        }

        private static DicomTag[] GetTags(string values)
        {
            var names = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return names.Select(n => IDicomToolkit.GetDicomTagByName(n)).ToArray();
        }
    }
}
