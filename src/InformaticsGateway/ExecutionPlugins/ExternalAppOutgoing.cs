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
using System.Threading;
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
    public class ExternalAppOutgoing : IOutputDataPlugin
    {
        private readonly ILogger<ExternalAppOutgoing> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly PluginConfiguration _options;

        public ExternalAppOutgoing(
            ILogger<ExternalAppOutgoing> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<PluginConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _options = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
            if (_options.Configuration.ContainsKey("ReplaceTags") is false) { throw new ArgumentNullException(nameof(configuration)); }
        }

        public async Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            var tags = GetTags(_options.Configuration["ReplaceTags"]);

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var remoteAppExecution = await GetRemoteAppExecution(exportRequestDataMessage, tags).ConfigureAwait(false);
            remoteAppExecution.StudyUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);

            foreach (var tag in tags)
            {
                if (dicomFile.Dataset.TryGetString(tag, out var value))
                {
                    remoteAppExecution.OriginalValues.Add(tag, value);
                    SetTag(dicomFile, tag);
                }
            }

            remoteAppExecution.OutgoingUid = dicomFile.Dataset.GetString(tags.First());

            await repository.AddAsync(remoteAppExecution).ConfigureAwait(false);
            _logger.LogStudyUidChanged(remoteAppExecution.StudyUid, remoteAppExecution.OutgoingUid);

            return (dicomFile, exportRequestDataMessage);
        }

        private static void SetTag(DicomFile dicomFile, DicomTag tag)
        {
            // partial implementation for now see
            // https://dicom.nema.org/dicom/2013/output/chtml/part05/sect_6.2.html
            // for full list

            switch (tag.DictionaryEntry.ValueRepresentations.First().Code)
            {
                case "UI":
                case "LO":
                case "LT":
                {
                    dicomFile.Dataset.AddOrUpdate(tag, DicomUIDGenerator.GenerateDerivedFromUUID());
                    break;
                }
                case "SH":
                case "AE":
                case "CS":
                case "PN":
                case "ST":
                case "UT":
                {
                    dicomFile.Dataset.AddOrUpdate(tag, "no Value");
                    break;
                }
            }
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
            remoteAppExecution.OutgoingUid = outgoingStudyUid;


            foreach (var destination in request.Destinations)
            {
                remoteAppExecution.ExportDetails.Add(await LookupDestinationAsync(destination, new CancellationToken()).ConfigureAwait(false));
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

        private static DicomTag[] GetTags(string values)
        {
            var names = values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return names.Select(n => IDicomToolkit.GetDicomTagByName(n)).ToArray();
        }

    }
}
