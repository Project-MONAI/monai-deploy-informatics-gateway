// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using System;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityManager : IApplicationEntityManager, IDisposable, IObserver<MonaiApplicationentityChangedEvent>
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceScope _serviceScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApplicationEntityManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, ApplicationEntityHandler> _aeTitles;
        private readonly IDisposable _unsubscriberForMonaiAeChangedNotificationService;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IFileSystem _fileSystem;
        private readonly IDicomToolkit _dicomToolkit;
        private bool _disposed = false;

        public IOptions<InformaticsGatewayConfiguration> Configuration { get; }

        public bool CanStore
        {
            get
            {
                return _storageInfoProvider.HasSpaceAvailableToStore;
            }
        }

        public ApplicationEntityManager(IHostApplicationLifetime applicationLifetime,
                                        IServiceScopeFactory serviceScopeFactory,
                                        IMonaiAeChangedNotificationService monaiAeChangedNotificationService,
                                        IOptions<InformaticsGatewayConfiguration> configuration,
                                        IStorageInfoProvider storageInfoProvider,
                                        IPayloadAssembler payloadAssembler,
                                        IFileSystem fileSystem,
                                        IDicomToolkit dicomToolkit)
        {
            _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));

            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _storageInfoProvider = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _dicomToolkit = dicomToolkit ?? throw new ArgumentNullException(nameof(dicomToolkit));

            _serviceScope = serviceScopeFactory.CreateScope();
            _serviceProvider = _serviceScope.ServiceProvider;

            _loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = _loggerFactory.CreateLogger<ApplicationEntityManager>();

            _unsubscriberForMonaiAeChangedNotificationService = monaiAeChangedNotificationService.Subscribe(this);
            _aeTitles = new ConcurrentDictionary<string, ApplicationEntityHandler>();
            _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            InitializeMonaiAeTitles();
        }

        ~ApplicationEntityManager() => Dispose(false);

        private void OnApplicationStopping()
        {
            _logger.Log(LogLevel.Information, "ApplicationEntityManager stopping.");
            _unsubscriberForMonaiAeChangedNotificationService.Dispose();
        }

        public async Task HandleCStoreRequest(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId)
        {
            Guard.Against.Null(request, nameof(request));

            if (!_aeTitles.ContainsKey(calledAeTitle))
            {
                throw new ArgumentException($"Called AE Title '{calledAeTitle}' is not configured");
            }

            if (!_storageInfoProvider.HasSpaceAvailableToStore)
            {
                throw new InsufficientStorageAvailableException($"Insufficient storage available.  Available storage space: {_storageInfoProvider.AvailableFreeSpace:D}");
            }

            var rootPath = _fileSystem.Path.Combine(Configuration.Value.Storage.TemporaryDataDirFullPath, calledAeTitle);
            var info = new DicomFileStorageInfo(associationId.ToString(), rootPath, request.MessageID.ToString(), callingAeTitle, _fileSystem);

            info.StudyInstanceUid = request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            info.SeriesInstanceUid = request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
            info.SopInstanceUid = request.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

            using (_logger.BeginScope("SOPInstanceUID={0}", info.SopInstanceUid))
            {
                _logger.Log(LogLevel.Information, "Study Instance UID: {StudyInstanceUid}", info.StudyInstanceUid);
                _logger.Log(LogLevel.Information, "Series Instance UID: {SeriesInstanceUid}", info.SeriesInstanceUid);
                _logger.Log(LogLevel.Information, "Storage File Path: {InstanceStorageFullPath}", info.FilePath);

                await _aeTitles[calledAeTitle].HandleInstance(request, info);
            }
        }

        public bool IsAeTitleConfigured(string calledAe)
        {
            return !string.IsNullOrWhiteSpace(calledAe) && _aeTitles.ContainsKey(calledAe);
        }

        public T GetService<T>()
        {
            return (T)_serviceProvider.GetService(typeof(T));
        }

        public ILogger GetLogger(string calledAeTitle)
        {
            return _loggerFactory.CreateLogger(calledAeTitle);
        }

        private void InitializeMonaiAeTitles()
        {
            _logger.Log(LogLevel.Information, "Loading MONAI Application Entities from data store.");

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<MonaiApplicationEntity>>();
            foreach (var ae in repository.AsQueryable())
            {
                AddNewAeTitle(ae);
            }
        }

        private void AddNewAeTitle(MonaiApplicationEntity entity)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var handler = new ApplicationEntityHandler(entity, _payloadAssembler, _dicomToolkit, _loggerFactory.CreateLogger<ApplicationEntityHandler>());
                if (!_aeTitles.TryAdd(entity.AeTitle, handler))
                {
                    _logger.Log(LogLevel.Error, $"AE Title {0} could not be added to CStore Manager.  Already exits: {1}", entity.AeTitle, _aeTitles.ContainsKey(entity.AeTitle));
                }
                else
                {
                    _logger.Log(LogLevel.Information, $"{entity.AeTitle} added to AE Title Manager");
                }
            }
        }

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
                    _serviceScope.Dispose();
                }
                _disposed = true;
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            _logger.Log(LogLevel.Error, error, "Error notifying observer.");
        }

        public void OnNext(MonaiApplicationentityChangedEvent applicationChangedEvent)
        {
            Guard.Against.Null(applicationChangedEvent, nameof(applicationChangedEvent));

            switch (applicationChangedEvent.Event)
            {
                case ChangedEventType.Added:
                    AddNewAeTitle(applicationChangedEvent.ApplicationEntity);
                    break;

                case ChangedEventType.Deleted:
                    _ = _aeTitles.TryRemove(applicationChangedEvent.ApplicationEntity.AeTitle, out _);
                    _logger.Log(LogLevel.Information, $"{applicationChangedEvent.ApplicationEntity.AeTitle} removed from AE Title Manager");
                    break;
            }
        }

        public bool IsValidSource(string callingAe, string host)
        {
            if (string.IsNullOrWhiteSpace(callingAe) || string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<SourceApplicationEntity>>();
            var sourceAe = repository.FirstOrDefault(p => p.AeTitle.Equals(callingAe) && p.HostIp.Equals(host, StringComparison.OrdinalIgnoreCase));

            if (sourceAe is null)
            {
                foreach (var src in repository.AsQueryable())
                {
                    _logger.Log(LogLevel.Information, $"Available source AET: {src.AeTitle} @ {src.HostIp}");
                }
            }

            return sourceAe is not null;
        }
    }
}
