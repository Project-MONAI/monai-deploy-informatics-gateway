/*
 * Copyright 2022 MONAI Consortium
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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
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
        private readonly ILogger<StreamsWriter> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IObjectUploadQueue _uploadQueue;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly DicomDataset _resultDicomDataset;
        private int _failureCount;
        private int _storedCount;

        public StreamsWriter(
            IObjectUploadQueue fileStore,
            IDicomToolkit dicomToolkit,
            IPayloadAssembler payloadAssembler,
            IOptions<InformaticsGatewayConfiguration> configuration,
            ILogger<StreamsWriter> logger,
            IFileSystem fileSystem)
        {
            _uploadQueue = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _resultDicomDataset = new DicomDataset();
            _failureCount = 0;
            _storedCount = 0;
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
                    if (stream.Length == 0)
                    {
                        _failureCount++;
                        _logger.ZeroLengthDicomWebStowStream();
                        continue;
                    }
                    await SaveInstance(stream, studyInstanceUid, workflowName, correlationId, dataSource, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.FailedToSaveInstance(ex);
                    AddFailure(DicomStatus.ProcessingFailure, null);
                }
            }

            if (_storedCount == 0 && _failureCount == 0)
            {
                return new StowResult { StatusCode = StatusCodes.Status204NoContent };
            }

            return new StowResult
            {
                StatusCode = GetStatusCode(streams.Count),
                Data = _resultDicomDataset
            };
        }

        private int GetStatusCode(int instancesReceived)
        {
            if (_failureCount == 0)
            {
                return StatusCodes.Status200OK;
            }
            else if (_failureCount == instancesReceived)
            {
                return StatusCodes.Status409Conflict;
            }
            else
            {
                return StatusCodes.Status202Accepted;
            }
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

            if (!string.IsNullOrWhiteSpace(studyInstanceUid) && !studyInstanceUid.Equals(uids.StudyInstanceUid, StringComparison.OrdinalIgnoreCase))
            {
                AddFailure(DicomStatus.StorageDataSetDoesNotMatchSOPClassWarning, uids);
                return;
            }

            var dicomInfo = new DicomFileStorageMetadata(correlationId, uids.Identifier, uids.StudyInstanceUid, uids.SeriesInstanceUid, uids.SopInstanceUid)
            {
                CalledAeTitle = string.Empty,
                Source = dataSource,
            };

            if (!string.IsNullOrWhiteSpace(workflowName))
            {
                dicomInfo.SetWorkflows(workflowName);
            }
            // for DICOMweb, use correlation ID as the grouping key
            var payloadId = await _payloadAssembler.Queue(correlationId, dicomInfo, _configuration.Value.DicomWeb.Timeout).ConfigureAwait(false);
            dicomInfo.PayloadId = payloadId.ToString();

            await dicomInfo.SetDataStreams(dicomFile, dicomFile.ToJson(_configuration.Value.Dicom.WriteDicomJson, _configuration.Value.Dicom.ValidateDicomOnSerialization), _configuration.Value.Storage.TemporaryDataStorage, _fileSystem, _configuration.Value.Storage.LocalTemporaryStoragePath).ConfigureAwait(false);
            _uploadQueue.Queue(dicomInfo);


            _logger.QueuedStowInstance();

            AddSuccess(null, uids);

            _storedCount++;
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
