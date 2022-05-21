// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    internal class ApplicationEntityManager : IApplicationEntityManager, IDisposable, IObserver<MonaiApplicationentityChangedEvent>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceScope _serviceScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApplicationEntityManager> _logger;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly ITemporaryFileStore _temporaryFileStore;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, ApplicationEntityHandler> _aeTitles;
        private readonly IDisposable _unsubscriberForMonaiAeChangedNotificationService;
        private readonly IStorageInfoProvider _storageInfoProvider;
        private readonly IPayloadAssembler _payloadAssembler;
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
                                        IOptions<InformaticsGatewayConfiguration> configuration,
                                        IStorageInfoProvider storageInfoProvider,
                                        IPayloadAssembler payloadAssembler)
        {
            if (applicationLifetime is null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }

            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _storageInfoProvider = storageInfoProvider ?? throw new ArgumentNullException(nameof(storageInfoProvider));
            _payloadAssembler = payloadAssembler ?? throw new ArgumentNullException(nameof(payloadAssembler));

            _serviceScope = serviceScopeFactory.CreateScope();
            _serviceProvider = _serviceScope.ServiceProvider;

            _loggerFactory = _serviceProvider.GetService<ILoggerFactory>() ?? throw new ServiceNotFoundException(nameof(ILoggerFactory));
            _logger = _loggerFactory.CreateLogger<ApplicationEntityManager>();

            _dicomToolkit = _serviceProvider.GetService<IDicomToolkit>() ?? throw new ServiceNotFoundException(nameof(IDicomToolkit));

            _temporaryFileStore = _serviceProvider.GetService<ITemporaryFileStore>() ?? throw new NullReferenceException(nameof(ITemporaryFileStore));

            _unsubscriberForMonaiAeChangedNotificationService = monaiAeChangedNotificationService.Subscribe(this);
            _aeTitles = new ConcurrentDictionary<string, ApplicationEntityHandler>();
            applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            InitializeMonaiAeTitles();
        }

        ~ApplicationEntityManager() => Dispose(false);

        private void OnApplicationStopping()
        {
            _logger.ApplicationEntityManagerStopping();
            _unsubscriberForMonaiAeChangedNotificationService.Dispose();
        }

#pragma warning disable S4457 // Parameter validation in "async"/"await" methods should be wrapped

        public async Task HandleCStoreRequest(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId)
#pragma warning restore S4457 // Parameter validation in "async"/"await" methods should be wrapped
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

            await HandleInstance(request, calledAeTitle, callingAeTitle, associationId).ConfigureAwait(false);
        }

        private async Task HandleInstance(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId)
        {
            var uids = _dicomToolkit.GetStudySeriesSopInstanceUids(request.File);

            using (_logger.BeginScope(new LoggingDataDictionary<string, object>() { { "SOPInstanceUID", uids.SopInstanceUid }, { "Correlation ID", associationId } }))
            {
                _logger.InstanceInformation(uids.StudyInstanceUid, uids.SeriesInstanceUid);

                await _aeTitles[calledAeTitle].HandleInstance(request, calledAeTitle, callingAeTitle, associationId, uids).ConfigureAwait(false);
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
            _logger.LoadingMonaiAeTitles();

            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IInformaticsGatewayRepository<MonaiApplicationEntity>>();
            foreach (var ae in repository.AsQueryable())
            {
                AddNewAeTitle(ae);
            }
        }

        private void AddNewAeTitle(MonaiApplicationEntity entity)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var handler = new ApplicationEntityHandler(entity, _payloadAssembler, _temporaryFileStore, _loggerFactory.CreateLogger<ApplicationEntityHandler>());
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
            Guard.Against.Null(applicationChangedEvent, nameof(applicationChangedEvent));

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
                    _logger.AvailableSource(src.AeTitle, src.HostIp);
                }
            }

            return sourceAe is not null;
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
