// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal interface IStreamsWriter
    {
        Task<StowResult> Save(IList<Stream> streams, string studyInstanceUid, string workflowName, string correlationId, string dataSource, CancellationToken cancellationToken = default);
    }

    internal class StreamsWriter : IStreamsWriter
    {
        private readonly IStorageInfoProvider _storageInfo;
        private readonly ILogger<StreamsWriter> _logger;
        private readonly ITemporaryFileStore _fileStore;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly DicomDataset _resultDicomDataset;
        private int _failureCount;

        public StreamsWriter(
            ITemporaryFileStore fileStore,
            IDicomToolkit dicomToolkit,
            IPayloadAssembler payloadAssembler,
            IOptions<InformaticsGatewayConfiguration> configuration,
            IStorageInfoProvider storageInfoProvider,
            ILogger<StreamsWriter> logger)
        {
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _storageInfo = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resultDicomDataset = new DicomDataset();
            _failureCount = 0;
        }

        public async Task<StowResult> Save(IList<Stream> streams, string studyInstanceUid, string workflowName, string correlationId, string dataSource, CancellationToken cancellationToken = default)
        {
            Guard.Against.NullOrEmpty(streams, nameof(streams));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(dataSource, nameof(dataSource));

            foreach (var stream in streams)
            {
                try
                {
                    await SaveInstance(stream, studyInstanceUid, workflowName, correlationId, dataSource, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.FailedToSaveInstance(ex);
                    AddFailure(DicomStatus.ProcessingFailure, null);
                }
            }

            return new StowResult
            {
                StatusCode = _failureCount == 0 ? StatusCodes.Status200OK : _failureCount == streams.Count ? StatusCodes.Status400BadRequest : StatusCodes.Status202Accepted,
                Data = _resultDicomDataset
            };
        }

        private async Task SaveInstance(Stream stream, string studyInstanceUid, string workflowName, string correlationId, string dataSource, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(stream, nameof(stream));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrWhiteSpace(dataSource, nameof(dataSource));

            stream.Seek(0, SeekOrigin.Begin);
            DicomFile dicomFile;
            try
            {
                dicomFile = await _dicomToolkit.OpenAsync(stream, FileReadOption.ReadAll).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.FailedToOpenStream(ex);
                AddFailure(DicomStatus.StorageCannotUnderstand, null);
                return;
            }

            var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(dicomFile);
            using var logger = _logger.BeginScope(new LoggingDataDictionary<string, object>
            {
                { "StudyInstanceUID", uids.StudyInstanceUid},
                { "SeriesInstanceUID", uids.SeriesInstanceUid},
                { "SOPInstanceUID", uids.SopInstanceUid}
            });

            DicomStoragePaths storagePaths;
            if (_storageInfo.HasSpaceAvailableToStore)
            {
                try
                {
                    storagePaths = await _fileStore.SaveDicomInstance(correlationId, dicomFile, cancellationToken).ConfigureAwait(false);
                }
                catch (IOException ex) when ((ex.HResult & 0xFFFF) == Constants.ERROR_HANDLE_DISK_FULL || (ex.HResult & 0xFFFF) == Constants.ERROR_DISK_FULL)
                {
                    _logger.StowFailedWithNoSpace(ex);
                    AddFailure(DicomStatus.StorageStorageOutOfResources, uids);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.FailedToSaveInstance(ex);
                    AddFailure(DicomStatus.ProcessingFailure, uids);
                    return;
                }
            }
            else
            {
                _logger.StowFailedWithNoSpace();
                AddFailure(DicomStatus.StorageStorageOutOfResources, uids);
                return;
            }

            var dicomInfo = new DicomFileStorageInfo
            {
                CalledAeTitle = string.Empty,
                CorrelationId = correlationId.ToString(),
                FilePath = storagePaths.FilePath,
                JsonFilePath = storagePaths.DicomMetadataFilePath,
                Id = uids.Identifier,
                Source = dataSource,
                StudyInstanceUid = uids.StudyInstanceUid,
                SeriesInstanceUid = uids.SeriesInstanceUid,
                SopInstanceUid = uids.SopInstanceUid,
            };

            if (!string.IsNullOrWhiteSpace(workflowName))
            {
                dicomInfo.SetWorkflows(workflowName);
            }

            // for DICOMweb, use correlation ID as the grouping key
            await _payloadAssembler.Queue(correlationId, dicomInfo, _configuration.Value.DicomWeb.Timeout).ConfigureAwait(false);
            _logger.QueuedInstanceUsingCorrelationId();

            if (!string.IsNullOrWhiteSpace(studyInstanceUid) && !studyInstanceUid.Equals(uids.StudyInstanceUid, StringComparison.OrdinalIgnoreCase))
            {
                AddSuccess(DicomStatus.StorageDataSetDoesNotMatchSOPClassWarning, uids);
            }
            else
            {
                AddSuccess(null, uids);
            }
        }

        private void AddSuccess(DicomStatus warningStatus = null, StudySerieSopUids uids = default)
        {
            if (!_resultDicomDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out var referencedSopSequence))
            {
                referencedSopSequence = new DicomSequence(DicomTag.ReferencedSOPSequence);

                _resultDicomDataset.Add(referencedSopSequence);
            }

            if (uids is not null)
            {
                var referencedItem = new DicomDataset
                {
                    { DicomTag.ReferencedSOPInstanceUID, uids.SopInstanceUid },
                    { DicomTag.ReferencedSOPClassUID, uids.SopClassUid },
                };

                if (warningStatus is not null)
                {
                    referencedItem.Add(DicomTag.WarningReason, warningStatus.Code);
                }

                referencedSopSequence.Items.Add(referencedItem);
            }
        }

        /// <inheritdoc />
        private void AddFailure(DicomStatus dicomStatus, StudySerieSopUids uids = default)
        {
            Guard.Against.Null(dicomStatus, nameof(dicomStatus));

            _failureCount++;

            if (!_resultDicomDataset.TryGetSequence(DicomTag.FailedSOPSequence, out var failedSopSequence))
            {
                failedSopSequence = new DicomSequence(DicomTag.FailedSOPSequence);
                _resultDicomDataset.Add(failedSopSequence);
            }

            if (uids is not null)
            {
                var failedItem = new DicomDataset
                {
                    { DicomTag.ReferencedSOPInstanceUID, uids.SopInstanceUid },
                    { DicomTag.ReferencedSOPClassUID, uids.SopClassUid },
                    { DicomTag.FailureReason, dicomStatus.Code },
                };

                failedSopSequence.Items.Add(failedItem);
            }
        }
    }
}
