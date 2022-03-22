// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityHandler
    {
        private readonly MonaiApplicationEntity _configuration;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly ITemporaryFileStore _fileStore;
        private readonly ILogger<ApplicationEntityHandler> _logger;

        public ApplicationEntityHandler(
            MonaiApplicationEntity monaiApplicationEntity,
            IPayloadAssembler payloadAssembler,
            ITemporaryFileStore fileStore,
            ILogger<ApplicationEntityHandler> logger)
        {
            _configuration = monaiApplicationEntity ?? throw new ArgumentNullException(nameof(monaiApplicationEntity));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal async Task HandleInstance(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId, StudySerieSopUids uids)
        {
            if (_configuration.IgnoredSopClasses.Contains(request.SOPClassUID.UID))
            {
                _logger.InstanceIgnoredWIthMatchingSopClassUid(request.SOPClassUID.UID);
                return;
            }

            var paths = await _fileStore.SaveDicomInstance(associationId.ToString(), request.File, CancellationToken.None).ConfigureAwait(false);
            var dicomInfo = new DicomFileStorageInfo
            {
                CalledAeTitle = calledAeTitle,
                CorrelationId = associationId.ToString(),
                FilePath = paths.FilePath,
                JsonFilePath = paths.DicomMetadataFilePath,
                Id = uids.Identifier,
                Source = callingAeTitle,
                StudyInstanceUid = uids.StudyInstanceUid,
                SeriesInstanceUid = uids.SeriesInstanceUid,
                SopInstanceUid = uids.SopInstanceUid,
            };

            if (_configuration.Workflows.Any())
            {
                dicomInfo.SetWorkflows(_configuration.Workflows.ToArray());
            }

            var dicomTag = FellowOakDicom.DicomTag.Parse(_configuration.Grouping);
            _logger.QueueInstanceUsingDicomTag(dicomTag);
            var key = request.Dataset.GetSingleValue<string>(dicomTag);
            await _payloadAssembler.Queue(key, dicomInfo, _configuration.Timeout).ConfigureAwait(false);
        }
    }
}
