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
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.ExecutionPlugins
{
    [PluginName("Remote App Execution Outgoing")]
    public class ExternalAppOutgoing : IOutputDataPlugin
    {
        private readonly ILogger<ExternalAppOutgoing> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly PluginConfiguration _options;

        public string Name => "Remote App Execution Outgoing";

        public ExternalAppOutgoing(
            ILogger<ExternalAppOutgoing> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<PluginConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _options = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
            if (_options.RemoteAppConfigurations.ContainsKey("ReplaceTags") is false) { throw new ArgumentNullException(nameof(configuration)); }
        }

        public async Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> Execute(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            var tags = IDicomToolkit.GetTagArrayFromStringArray(_options.RemoteAppConfigurations["ReplaceTags"]);
            var outgoingUid = dicomFile.Dataset.GetString(tags[0]);

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var existing = await repository.GetAsync(outgoingUid).ConfigureAwait(false);

            if (existing is not null)
            {
                PopulateWithStoredProxyValues(dicomFile, tags, existing);
            }
            else
            {
                var remoteAppExecution = await PopulateWithNewProxyValues(dicomFile, exportRequestDataMessage, tags).ConfigureAwait(false);
                await repository.AddAsync(remoteAppExecution).ConfigureAwait(false);
            }
            return (dicomFile, exportRequestDataMessage);
        }

        private async Task<RemoteAppExecution> PopulateWithNewProxyValues(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage, DicomTag[] tags)
        {
            var remoteAppExecution = await GetRemoteAppExecution(exportRequestDataMessage).ConfigureAwait(false);
            remoteAppExecution.StudyUid = dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID);

            foreach (var tag in tags)
            {
                if (dicomFile.Dataset.TryGetString(tag, out var value))
                {
                    remoteAppExecution.OriginalValues.Add(tag.ToString(), value);
                    var newValue = IDicomToolkit.GetTagProxyValue<string>(tag);
                    dicomFile.Dataset.AddOrUpdate(tag, newValue);
                    remoteAppExecution.ProxyValues.Add(tag.ToString(), newValue);
                }
            }

            remoteAppExecution.OutgoingUid = dicomFile.Dataset.GetString(tags[0]);
            _logger.LogStudyUidChanged(remoteAppExecution.StudyUid, remoteAppExecution.OutgoingUid);
            return remoteAppExecution;
        }

        private static void PopulateWithStoredProxyValues(DicomFile dicomFile, DicomTag[] tags, RemoteAppExecution existing)
        {
            foreach (var tag in tags)
            {
                if (dicomFile.Dataset.TryGetString(tag, out _))
                {
                    dicomFile.Dataset.AddOrUpdate(tag, existing.ProxyValues[tag.ToString()]);
                }
            }
        }

        private async Task<RemoteAppExecution> GetRemoteAppExecution(ExportRequestDataMessage request)
        {
            var remoteAppExecution = new RemoteAppExecution
            {
                CorrelationId = request.CorrelationId,
                WorkflowInstanceId = request.WorkflowInstanceId,
                ExportTaskId = request.ExportTaskId,
                Files = new System.Collections.Generic.List<string> { request.Filename },
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

            return destination ?? throw new ConfigurationException($"Specified destination '{destinationName}' does not exist.");
        }
    }
}
