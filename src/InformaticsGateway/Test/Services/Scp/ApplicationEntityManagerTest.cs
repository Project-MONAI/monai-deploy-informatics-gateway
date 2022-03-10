﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
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
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Shared.Test;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class ApplicationEntityManagerTest
    {
        private readonly Mock<IHostApplicationLifetime> _hostApplicationLifetime;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger<ApplicationEntityManager>> _logger;
        private readonly Mock<ILogger<MonaiAeChangedNotificationService>> _loggerNotificationService;
        private readonly Mock<IPayloadAssembler> _fileStoredNotificationQueue;

        private readonly IMonaiAeChangedNotificationService _monaiAeChangedNotificationService;
        private readonly Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>> _applicationEntityRepository;
        private readonly Mock<IInformaticsGatewayRepository<SourceApplicationEntity>> _sourceEntityRepository;
        private readonly IOptions<InformaticsGatewayConfiguration> _connfiguration;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IDicomToolkit _dicomToolkit;

        private readonly IServiceProvider _serviceProvider;

        public ApplicationEntityManagerTest()
        {
            _hostApplicationLifetime = new Mock<IHostApplicationLifetime>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<ApplicationEntityManager>>();
            _fileStoredNotificationQueue = new Mock<IPayloadAssembler>();
            _loggerNotificationService = new Mock<ILogger<MonaiAeChangedNotificationService>>();
            _monaiAeChangedNotificationService = new MonaiAeChangedNotificationService(_loggerNotificationService.Object);
            _applicationEntityRepository = new Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>>();
            _sourceEntityRepository = new Mock<IInformaticsGatewayRepository<SourceApplicationEntity>>();
            _connfiguration = Options.Create(new InformaticsGatewayConfiguration());
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _fileSystem = new MockFileSystem();
            _dicomToolkit = new DicomToolkit(_fileSystem);

            var services = new ServiceCollection();
            services.AddScoped(p => _loggerFactory.Object);
            services.AddScoped(p => _fileStoredNotificationQueue.Object);
            services.AddScoped(p => _applicationEntityRepository.Object);
            services.AddScoped(p => _sourceEntityRepository.Object);

            _serviceProvider = services.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns((string type) =>
            {
                return _logger.Object;
            });
        }

        [RetryFact(5, 250, DisplayName = "HandleCStoreRequest - Shall throw if AE Title not configured")]
        public async Task HandleCStoreRequest_ShallThrowIfAENotConfigured()
        {
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            var request = GenerateRequest();
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await manager.HandleCStoreRequest(request, "BADAET", "CallingAET", Guid.NewGuid());
            });

            Assert.Equal("Called AE Title 'BADAET' is not configured", exception.Message);
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToStore, Times.Never());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "HandleCStoreRequest - Shall ignored matching SOP Classes")]
        public async Task HandleCStoreRequest_ShallIgnoredMatchingSopClasses()
        {
            var aet = "TESTAET";

            var data = new List<MonaiApplicationEntity>()
            {
                new MonaiApplicationEntity()
                {
                    AeTitle = aet,
                    Name =aet,
                    Workflows = new List<string>(){ "AppA", "AppB", Guid.NewGuid().ToString() },
                    IgnoredSopClasses = new List<string>{ DicomUID.SecondaryCaptureImageStorage.UID }
                }
            };
            _applicationEntityRepository.Setup(p => p.AsQueryable()).Returns(data.AsQueryable());
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);
            _fileStoredNotificationQueue.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()));
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            var request = GenerateRequest();
            await manager.HandleCStoreRequest(request, aet, "CallingAET", Guid.NewGuid());

            _logger.VerifyLogging($"{aet} added to AE Title Manager", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Study Instance UID: {request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)}", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Series Instance UID: {request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID)}", LogLevel.Information, Times.Once());

            _logger.VerifyLoggingMessageBeginsWith($"Instance ignored due to matching SOP Class UID {DicomUID.SecondaryCaptureImageStorage.UID}", LogLevel.Information, Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Preparing to save", LogLevel.Debug, Times.Never());
            _logger.VerifyLoggingMessageBeginsWith($"Instanced saved", LogLevel.Information, Times.Never());

            _applicationEntityRepository.Verify(p => p.AsQueryable(), Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToStore, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());

            var fileSystem = _fileSystem as MockFileSystem;
            Assert.Empty(fileSystem.AllFiles);
        }

        [RetryFact(5, 250, DisplayName = "HandleCStoreRequest - Shall save instance and notify")]
        public async Task HandleCStoreRequest_ShallSaveInstanceAndNotify()
        {
            var aet = "TESTAET";

            var data = new List<MonaiApplicationEntity>()
            {
                new MonaiApplicationEntity()
                {
                    AeTitle = aet,
                    Name =aet,
                    Workflows = new List<string>(){ "AppA", "AppB", Guid.NewGuid().ToString() }
                }
            };
            _applicationEntityRepository.Setup(p => p.AsQueryable()).Returns(data.AsQueryable());
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(true);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);
            _fileStoredNotificationQueue.Setup(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()));
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            var request = GenerateRequest();
            await manager.HandleCStoreRequest(request, aet, "CallingAET", Guid.NewGuid());

            _logger.VerifyLogging($"{aet} added to AE Title Manager", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Study Instance UID: {request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)}", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"Series Instance UID: {request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID)}", LogLevel.Information, Times.Once());

            _logger.VerifyLoggingMessageBeginsWith($"Preparing to save", LogLevel.Debug, Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Instance saved", LogLevel.Information, Times.Once());

            _applicationEntityRepository.Verify(p => p.AsQueryable(), Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToStore, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.Never());

            var fileSystem = _fileSystem as MockFileSystem;
            Assert.Single(fileSystem.AllFiles.Where(p => p.EndsWith(DicomFileStorageInfo.FilExtension)));
            Assert.Single(fileSystem.AllFiles.Where(p => p.EndsWith(DicomFileStorageInfo.DicomJsonFileExtension)));
            var stream = fileSystem.File.OpenRead(fileSystem.AllFiles.First());
            var dicom = DicomFile.Open(stream, FileReadOption.ReadAll);

            Assert.Equal(request.SOPClassUID.UID, dicom.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID));
            Assert.Equal(request.SOPInstanceUID.UID, dicom.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.Equal(request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID), dicom.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
            Assert.Equal(request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID), dicom.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
            Assert.Equal(request.Dataset.GetSingleValue<string>(DicomTag.PatientID), dicom.Dataset.GetSingleValue<string>(DicomTag.PatientID));
        }

        [RetryFact(5, 250, DisplayName = "HandleCStoreRequest - Throws when available storage space is low")]
        public async Task HandleCStoreRequest_ThrowWhenOnLowStorageSpace()
        {
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(false);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);
            var aet = "TESTAET";

            var data = new List<MonaiApplicationEntity>()
            {
                new MonaiApplicationEntity()
                {
                    AeTitle = aet,
                    Name =aet
                }
            };
            _applicationEntityRepository.Setup(p => p.AsQueryable()).Returns(data.AsQueryable());
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            var request = GenerateRequest();
            await Assert.ThrowsAsync<InsufficientStorageAvailableException>(async () =>
            {
                await manager.HandleCStoreRequest(request, aet, "CallingAET", Guid.NewGuid());
            });

            _logger.VerifyLogging($"{aet} added to AE Title Manager", LogLevel.Information, Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Preparing to save:", LogLevel.Debug, Times.Never());
            _logger.VerifyLoggingMessageBeginsWith($"Instanced saved", LogLevel.Information, Times.Never());
            _logger.VerifyLoggingMessageBeginsWith($"Instance queued for upload", LogLevel.Information, Times.Never());

            _applicationEntityRepository.Verify(p => p.AsQueryable(), Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToStore, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.AtLeastOnce());
            _fileStoredNotificationQueue.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageInfo>()), Times.Never());
        }

        [RetryFact(5, 250, DisplayName = "IsAeTitleConfigured")]
        public void IsAeTitleConfigured()
        {
            var aet = "TESTAET";
            var data = new List<MonaiApplicationEntity>()
            {
                new MonaiApplicationEntity()
                {
                    AeTitle = aet,
                    Name =aet
                }
            };
            _applicationEntityRepository.Setup(p => p.AsQueryable()).Returns(data.AsQueryable());
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            Assert.True(manager.IsAeTitleConfigured(aet));
            Assert.False(manager.IsAeTitleConfigured("BAD"));
        }

        [RetryFact(5, 250, DisplayName = "GetService - Shall return request service")]
        public void GetService_ShallReturnRequestedServicec()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            Assert.Equal(manager.GetService<ILoggerFactory>(), _loggerFactory.Object);
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - False when AE is empty or white space")]
        public void IsValidSource_FalseWhenAEIsEmpty()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            Assert.False(manager.IsValidSource("  ", "123"));
            Assert.False(manager.IsValidSource("AAA", ""));
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - False when no matching source found")]
        public void IsValidSource_FalseWhenNoMatchingSource()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            _sourceEntityRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()))
                .Returns(default(SourceApplicationEntity));
            _sourceEntityRepository.Setup(p => p.AsQueryable()).Returns(
                (new List<SourceApplicationEntity>
                    { new SourceApplicationEntity { AeTitle = "SAE", HostIp = "1.2.3.4", Name = "SAE" } }
                ).AsQueryable());

            var sourceAeTitle = "ValidSource";
            Assert.False(manager.IsValidSource(sourceAeTitle, "1.2.3.4"));

            _sourceEntityRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()), Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Available source AET: SAE @ 1.2.3.4", LogLevel.Information, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - False when IP does not match")]
        public void IsValidSource_FalseWithMismatchIp()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            var aet = "SAE";
            _sourceEntityRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()))
                .Returns(default(SourceApplicationEntity));

            Assert.False(manager.IsValidSource(aet, "1.1.1.1"));

            _sourceEntityRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - True")]
        public void IsValidSource_True()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            var aet = "SAE";
            _sourceEntityRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()))
                .Returns(new SourceApplicationEntity { AeTitle = aet, HostIp = "1.2.3.4", Name = "SAE" });

            Assert.True(manager.IsValidSource(aet, "1.2.3.4"));

            _sourceEntityRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Shall handle AE change events")]
        public void ShallHandleAEChangeEvents()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration,
                                                       _storageInfoProvider.Object,
                                                       _fileStoredNotificationQueue.Object,
                                                       _fileSystem,
                                                       _dicomToolkit);

            _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(
                new MonaiApplicationEntity
                {
                    AeTitle = "AE1",
                    Name = "AE1"
                }, ChangedEventType.Added));
            Assert.True(manager.IsAeTitleConfigured("AE1"));

            _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(
                new MonaiApplicationEntity
                {
                    AeTitle = "AE1",
                    Name = "AE1"
                }, ChangedEventType.Deleted));
            Assert.False(manager.IsAeTitleConfigured("AE1"));
        }

        private static DicomCStoreRequest GenerateRequest()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var file = new DicomFile(dataset);
            return new DicomCStoreRequest(file);
        }
    }
}
