/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityManager : IApplicationEntityManager, IDisposable, IObserver<MonaiApplicationentityChangedEvent>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<ApplicationEntityManager> _logger;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly ConcurrentDictionary<string, IApplicationEntityHandler> _aeTitles;
        private readonly IDisposable _unsubscriberForMonaiAeChangedNotificationService;
        private readonly Task _initializeTask;
        private bool _disposedValue;

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
                                        IOptions<InformaticsGatewayConfiguration> configuration)
        {
            if (applicationLifetime is null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _serviceScope = serviceScopeFactory.CreateScope();
            var serviceProvider = _serviceScope.ServiceProvider;

            _loggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? throw new ServiceNotFoundException(nameof(ILoggerFactory));
            _logger = _loggerFactory.CreateLogger<ApplicationEntityManager>();

            _dicomToolkit = serviceProvider.GetService<IDicomToolkit>() ?? throw new ServiceNotFoundException(nameof(IDicomToolkit));
            _storageInfoProvider = serviceProvider.GetService<IStorageInfoProvider>() ?? throw new ServiceNotFoundException(nameof(IStorageInfoProvider));
            _unsubscriberForMonaiAeChangedNotificationService = monaiAeChangedNotificationService.Subscribe(this);
            _aeTitles = new ConcurrentDictionary<string, IApplicationEntityHandler>();
            applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            _initializeTask = InitializeMonaiAeTitlesAsync();
        }

        ~ApplicationEntityManager() => Dispose(false);

        private void OnApplicationStopping()
        {
            _logger.ApplicationEntityManagerStopping();
            _unsubscriberForMonaiAeChangedNotificationService.Dispose();
        }

        public async Task HandleCStoreRequest(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId)
        {
            Guard.Against.Null(request);

            await _initializeTask.ConfigureAwait(false);

            if (!_aeTitles.ContainsKey(calledAeTitle))
            {
                throw new ArgumentException($"Called AE Title '{calledAeTitle}' is not configured");
            }

            if (!_storageInfoProvider.HasSpaceAvailableToStore)
            {
                throw new InsufficientStorageAvailableException($"Insufficient storage available.  Available storage space: {_storageInfoProvider.AvailableFreeSpace:D}");
            }

            await HandleInstance(request, calledAeTitle, callingAeTitle, associationId).ConfigureAwait(false);
        }

        private async Task HandleInstance(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId)
        {
            var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(request.File);

            using (_logger.BeginScope(new LoggingDataDictionary<string, object>() { { "SOPInstanceUID", uids.SopInstanceUid }, { "CorrelationId", associationId } }))
            {
                _logger.InstanceInformation(uids.StudyInstanceUid, uids.SeriesInstanceUid);

                await _aeTitles[calledAeTitle].HandleInstanceAsync(request, calledAeTitle, callingAeTitle, associationId, uids).ConfigureAwait(false);
            }
        }

        public async Task<bool> IsAeTitleConfiguredAsync(string calledAe)
        {
            Guard.Against.NullOrWhiteSpace(calledAe);
            await _initializeTask.ConfigureAwait(false);

            return _aeTitles.ContainsKey(calledAe);
        }

        public T GetService<T>()
        {
            return (T)_serviceScope.ServiceProvider.GetService(typeof(T));
        }

        public ILogger GetLogger(string calledAeTitle)
        {
            return _loggerFactory.CreateLogger(calledAeTitle);
        }

        private async Task InitializeMonaiAeTitlesAsync()
        {
            _logger.LoadingMonaiAeTitles();

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMonaiApplicationEntityRepository>();
            foreach (var ae in await repository.ToListAsync().ConfigureAwait(false))
            {
                AddNewAeTitle(ae);
            }
        }

        private void AddNewAeTitle(MonaiApplicationEntity entity)
        {
            Guard.Against.Null(entity);

            var scope = _serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetService<IApplicationEntityHandler>() ?? throw new ServiceNotFoundException(nameof(IApplicationEntityHandler));
            handler.Configure(entity, Configuration.Value.Dicom.WriteDicomJson, Configuration.Value.Dicom.ValidateDicomOnSerialization);

            if (!_aeTitles.TryAdd(entity.AeTitle, handler))
            {
                _logger.AeTitleCannotBeAdded(entity.AeTitle, _aeTitles.ContainsKey(entity.AeTitle));
            }
            else
            {
                _logger.AeTitleAdded(entity.AeTitle);
            }
        }

        public void OnCompleted()
        {
            // noop
        }

        public void OnError(Exception error)
        {
            _logger.ErrorNotifyingObserver();
        }

        public void OnNext(MonaiApplicationentityChangedEvent applicationChangedEvent)
        {
            Guard.Against.Null(applicationChangedEvent);

            switch (applicationChangedEvent.Event)
            {
                case ChangedEventType.Added:
                    AddNewAeTitle(applicationChangedEvent.ApplicationEntity);
                    break;

                case ChangedEventType.Deleted:
                    _ = _aeTitles.TryRemove(applicationChangedEvent.ApplicationEntity.AeTitle, out _);
                    _logger.AeTitleRemoved(applicationChangedEvent.ApplicationEntity.AeTitle);
                    break;
            }
        }

        public async Task<bool> IsValidSourceAsync(string callingAe, string host)
        {
            if (string.IsNullOrWhiteSpace(callingAe) || string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISourceApplicationEntityRepository>();
            var containsSource = await repository.ContainsAsync(p => p.AeTitle.Equals(callingAe) && p.HostIp.Equals(host)).ConfigureAwait(false);

            if (!containsSource)
            {
                foreach (var src in await repository.ToListAsync().ConfigureAwait(false))
                {
                    _logger.AvailableSource(src.AeTitle, src.HostIp);
                }
            }

            return containsSource;
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
