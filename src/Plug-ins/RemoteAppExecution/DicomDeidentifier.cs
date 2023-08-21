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
using Ardalis.GuardClauses;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution
{
    [PlugInName("Remote App Execution Outgoing")]
    public class DicomDeidentifier : IOutputDataPlugIn
    {
        private readonly ILogger<DicomDeidentifier> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly PlugInConfiguration _options;

        public string Name => GetType().GetCustomAttribute<PlugInNameAttribute>()?.Name ?? GetType().Name;

        public DicomDeidentifier(
            ILogger<DicomDeidentifier> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<PlugInConfiguration> configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _options = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

            if (_options.RemoteAppConfigurations.ContainsKey(SR.ConfigKey_ReplaceTags) is false)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
        }

        public async Task<(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)> ExecuteAsync(DicomFile dicomFile, ExportRequestDataMessage exportRequestDataMessage)
        {
            Guard.Against.Null(dicomFile, nameof(dicomFile));
            Guard.Against.Null(exportRequestDataMessage, nameof(exportRequestDataMessage));

            var tags = Utilities.GetTagArrayFromStringArray(_options.RemoteAppConfigurations[SR.ConfigKey_ReplaceTags]);
            var studyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            var seriesInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);

            var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRemoteAppExecutionRepository>();

            var existing = await repository.GetAsync(exportRequestDataMessage.WorkflowInstanceId, exportRequestDataMessage.ExportTaskId, studyInstanceUid, seriesInstanceUid).ConfigureAwait(false);

            var newRecord = new RemoteAppExecution(exportRequestDataMessage, existing?.StudyInstanceUid, existing?.SeriesInstanceUid);

            newRecord.OriginalValues.Add(DicomTag.StudyInstanceUID.ToString(), studyInstanceUid);
            newRecord.OriginalValues.Add(DicomTag.SeriesInstanceUID.ToString(), seriesInstanceUid);
            newRecord.OriginalValues.Add(DicomTag.SOPInstanceUID.ToString(), dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));

            dicomFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, newRecord.StudyInstanceUid);
            dicomFile.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, newRecord.SeriesInstanceUid);
            dicomFile.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID, newRecord.SopInstanceUid);

            foreach (var tag in tags)
            {
                if (tag.Equals(DicomTag.StudyInstanceUID) ||
                    tag.Equals(DicomTag.SeriesInstanceUID) ||
                    tag.Equals(DicomTag.SOPInstanceUID))
                {
                    continue;
                }

                if (dicomFile.Dataset.TryGetString(tag, out var value))
                {
                    newRecord.OriginalValues.Add(tag.ToString(), value);
                    var newValue = Utilities.GetTagProxyValue<string>(tag);
                    dicomFile.Dataset.AddOrUpdate(tag, newValue);
                    _logger.ValueChanged(tag.ToString(), value, newValue);
                }
            }

            await repository.AddAsync(newRecord).ConfigureAwait(false);

            return (dicomFile, exportRequestDataMessage);
        }
    }
}
