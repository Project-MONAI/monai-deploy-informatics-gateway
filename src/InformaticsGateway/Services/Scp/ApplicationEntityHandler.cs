/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityHandler : IDisposable, IApplicationEntityHandler
    {
        private readonly ILogger<ApplicationEntityHandler> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly IServiceScope _serviceScope;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IObjectUploadQueue _uploadQueue;
        private readonly IFileSystem _fileSystem;
        private MonaiApplicationEntity _configuration;
        private DicomJsonOptions _dicomJsonOptions;
        private bool _validateDicomValueOnJsonSerialization;
        private bool _disposedValue;

        public ApplicationEntityHandler(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ApplicationEntityHandler> logger,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            Guard.Against.Null(serviceScopeFactory, nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _serviceScope = serviceScopeFactory.CreateScope();
            _payloadAssembler = _serviceScope.ServiceProvider.GetService<IPayloadAssembler>() ?? throw new ServiceNotFoundException(nameof(IPayloadAssembler));
            _uploadQueue = _serviceScope.ServiceProvider.GetService<IObjectUploadQueue>() ?? throw new ServiceNotFoundException(nameof(IObjectUploadQueue));
            _fileSystem = _serviceScope.ServiceProvider.GetService<IFileSystem>() ?? throw new ServiceNotFoundException(nameof(IFileSystem));
        }

        public void Configure(MonaiApplicationEntity monaiApplicationEntity, DicomJsonOptions dicomJsonOptions, bool validateDicomValuesOnJsonSerialization)
        {
            Guard.Against.Null(monaiApplicationEntity, nameof(monaiApplicationEntity));

            _configuration = monaiApplicationEntity;
            _dicomJsonOptions = dicomJsonOptions;
            _validateDicomValueOnJsonSerialization = validateDicomValuesOnJsonSerialization;
        }

        public async Task HandleInstanceAsync(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId, StudySerieSopUids uids)
        {
            if (_configuration is null)
            {
                throw new NotSupportedException("Must call Configure(...) first.");
            }

            Guard.Against.Null(request, nameof(request));
            Guard.Against.NullOrWhiteSpace(calledAeTitle, nameof(calledAeTitle));
            Guard.Against.NullOrWhiteSpace(callingAeTitle, nameof(callingAeTitle));
            Guard.Against.Null(associationId, nameof(associationId));
            Guard.Against.Null(uids, nameof(uids));

            if (!AcceptsSopClass(uids.SopClassUid))
            {
                _logger.InstanceIgnoredWIthMatchingSopClassUid(request.SOPClassUID.UID);
                return;
            }

            var dicomInfo = new DicomFileStorageMetadata(associationId.ToString(), uids.Identifier, uids.StudyInstanceUid, uids.SeriesInstanceUid, uids.SopInstanceUid)
            {
                CalledAeTitle = calledAeTitle,
                Source = callingAeTitle,
            };

            if (_configuration.Workflows.Any())
            {
                dicomInfo.SetWorkflows(_configuration.Workflows.ToArray());
            }

            await dicomInfo.SetDataStreams(request.File, request.File.ToJson(_dicomJsonOptions, _validateDicomValueOnJsonSerialization), _options.Value.Storage.TemporaryDataStorage, _fileSystem, _options.Value.Storage.LocalTemporaryStoragePath).ConfigureAwait(false);
            _uploadQueue.Queue(dicomInfo);

            var dicomTag = FellowOakDicom.DicomTag.Parse(_configuration.Grouping);
            _logger.QueueInstanceUsingDicomTag(dicomTag);
            var key = request.Dataset.GetSingleValue<string>(dicomTag);
            await _payloadAssembler.Queue(key, dicomInfo, _configuration.Timeout).ConfigureAwait(false);
        }

        private bool AcceptsSopClass(string sopClassUid)
        {
            Guard.Against.NullOrWhiteSpace(sopClassUid);

            if (_configuration.IgnoredSopClasses.Any())
            {
                return !_configuration.IgnoredSopClasses.Contains(sopClassUid);
            }

            if (_configuration.AllowedSopClasses.Any())
            {
                return _configuration.AllowedSopClasses.Contains(sopClassUid);
            }

            return true; // always accept if non of the allowed/ignored list were defined.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _serviceScope.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
