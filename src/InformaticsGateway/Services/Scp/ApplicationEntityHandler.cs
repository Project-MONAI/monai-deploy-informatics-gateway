// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using System;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityHandler : IDisposable
    {
        private readonly object _syncRoot = new object();
        private bool _disposed = false;
        private IServiceScopeFactory _serviceScopeFactory;
        private MonaiApplicationEntity _configuration;
        private IStorageInfoProvider _storageInfoProvider;
        private IPayloadAssembler _payloadAssembler;
        private IFileSystem _fileSystem;
        private IDicomToolkit _dicomToolkit;
        private ILogger<ApplicationEntityHandler> _logger;

        public ApplicationEntityHandler(
            IServiceScopeFactory serviceScopeFactory,
            MonaiApplicationEntity monaiApplicationEntity,
            IStorageInfoProvider storageInfoProvider,
            IPayloadAssembler payloadAssembler,
            IFileSystem fileSystem,
            IDicomToolkit dicomToolkit,
            ILogger<ApplicationEntityHandler> logger)
        {

            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _configuration = monaiApplicationEntity ?? throw new ArgumentNullException(nameof(monaiApplicationEntity));
            _storageInfoProvider = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        ~ApplicationEntityHandler() => Dispose(false);


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
                _disposed = true;
            }
        }

        internal async Task HandleInstance(DicomCStoreRequest request, DicomFileStorageInfo info)
        {
            if (_configuration.IgnoredSopClasses.Contains(request.SOPClassUID.UID))
            {
                _logger.Log(LogLevel.Information, $"Instance ignored due to matching SOP Class UID {request.SOPClassUID.UID}");
                return;
            }

            if (_configuration.Workflows.Any())
            {
                info.SetWorkflows(_configuration.Workflows.ToArray());
            }

            await SaveDicomInstance(request, info.FilePath);

            _payloadAssembler.Queue(_configuration.Grouping, info, _configuration.Timeout);
        }


        private async Task NotifyStoredInstance(FileStorageInfo file)
        {
            _logger.Log(LogLevel.Information, $"Instance queued for upload: {file.FilePath}");
        }

        private async Task SaveDicomInstance(DicomCStoreRequest request, string filename)
        {
            _logger.Log(LogLevel.Debug, $"Preparing to save {filename}");
            await _dicomToolkit.Save(request.File, filename);
            _logger.Log(LogLevel.Information, $"Instanced saved {filename}");
        }
    }
}
