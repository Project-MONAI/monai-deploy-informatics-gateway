/*
 * Copyright 2021-2023 MONAI Consortium
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
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.ExecutionPlugins;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Moq;
using Xunit;
using System.Threading.Tasks;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using System.Threading;
using Monai.Deploy.InformaticsGateway.Configuration;
using Microsoft.Extensions.Options;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class ExternalAppPluginTest
    {
        private readonly Mock<ILogger<ExternalAppIncoming>> _logger;
        private readonly Mock<ILogger<ExternalAppOutgoing>> _loggerOut;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IRemoteAppExecutionRepository> _repository;
        private readonly Mock<IDestinationApplicationEntityRepository> _destRepo;
        private readonly ServiceProvider _serviceProvider;
        private readonly PluginConfiguration _pluginOptions;
        private readonly ServiceCollection _serviceCollection;

        public ExternalAppPluginTest()
        {
            _logger = new Mock<ILogger<ExternalAppIncoming>>();
            _loggerOut = new Mock<ILogger<ExternalAppOutgoing>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _repository = new Mock<IRemoteAppExecutionRepository>();
            _destRepo = new Mock<IDestinationApplicationEntityRepository>();
            _destRepo.Setup(d => d.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new DestinationApplicationEntity()));

            _pluginOptions = new PluginConfiguration { Configuration = { { "ReplaceTags", "SOPClassUID, StudyInstanceUID, AccessionNumber, SeriesInstanceUID, SOPInstanceUID" } } };

            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddScoped(p => _logger.Object);
            _serviceCollection.AddScoped(p => _loggerOut.Object);
            _serviceCollection.AddScoped(p => _repository.Object);
            _serviceCollection.AddScoped(p => _destRepo.Object);
            _serviceCollection.AddOptions<PluginConfiguration>().Configure(options => options = _pluginOptions);
            _serviceCollection.PostConfigure<PluginConfiguration>(opts =>
            {
                opts.Configuration = _pluginOptions.Configuration;
            });

            _serviceProvider = _serviceCollection.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenAnExternalAppIncoming_WhenInitialized_ExpectParametersToBeValidated()
        {
            var options = Options.Create<PluginConfiguration>(new PluginConfiguration
            {
                Configuration = { { "ReplaceTags", "SOPClassUID" } }
            });
            Assert.Throws<ArgumentNullException>(() => new ExternalAppIncoming(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppIncoming(null, _serviceScopeFactory.Object, options));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppIncoming(null, null, options));

            _ = new ExternalAppIncoming(_logger.Object, _serviceScopeFactory.Object, options);
        }

        [Fact]
        public void GivenAnExternalAppOutgoing_WhenInitialized_ExpectParametersToBeValidated()
        {
            var options = Options.Create<PluginConfiguration>(new PluginConfiguration
            {
                Configuration = { { "ReplaceTags", "SOPClassUID" } }
            });
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(null, _serviceScopeFactory.Object, options));
            Assert.Throws<ArgumentNullException>(() => new ExternalAppOutgoing(null, _serviceScopeFactory.Object, options));

            _ = new ExternalAppOutgoing(_loggerOut.Object, _serviceScopeFactory.Object, options);
        }

        [Fact]
        public void GivenAnOutputDataPluginEngine_WhenConfigureIsCalledWithAValidAssembly_ExpectNoExceptions()
        {
            var pluginEngine = new OutputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<OutputDataPluginEngine>>().Object,
                new Mock<IDicomToolkit>().Object);

            var assemblies = new List<string>() {
                typeof(ExternalAppOutgoing).AssemblyQualifiedName};

            pluginEngine.Configure(assemblies);
        }

        [Fact]
        public void GivenAnInputDataPluginEngine_WhenConfigureIsCalledWithAValidAssembly_ExpectNoExceptions()
        {
            var pluginEngine = new InputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<InputDataPluginEngine>>().Object);

            var assemblies = new List<string>() {
                typeof(ExternalAppIncoming).AssemblyQualifiedName};

            pluginEngine.Configure(assemblies);
        }

        [Fact]
        public async Task ExternalAppPlugin_Should_Replace_StudyUid_Plus_SaveData()
        {
            var toolkit = new Mock<IDicomToolkit>();

            RemoteAppExecution localCopy = new RemoteAppExecution();

            _repository.Setup(r => r.AddAsync(It.IsAny<RemoteAppExecution>(), It.IsAny<CancellationToken>()))
                .Callback((RemoteAppExecution item, CancellationToken c) =>
                localCopy = item
                );

            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.AccessionNumber, "AccesssionNumber" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var dicomFile = new DicomFile(dataset);

            var originalStudyUid = dataset.GetString(DicomTag.StudyInstanceUID);

            toolkit.Setup(t => t.Load(It.IsAny<byte[]>())).Returns(dicomFile);

            var pluginEngine = new OutputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<OutputDataPluginEngine>>().Object,
                toolkit.Object);
            pluginEngine.Configure(new List<string>() { typeof(ExternalAppOutgoing).AssemblyQualifiedName });

            string[] destinations = { "fred" };

            var exportMessage = new ExportRequestDataMessage(new Messaging.Events.ExportRequestEvent() { Destinations = destinations }, "");

            var exportRequestDataMessage = await pluginEngine.ExecutePlugins(exportMessage);

            Assert.Equal(originalStudyUid, localCopy.StudyUid);
            Assert.Equal(dataset.GetString(DicomTag.SOPClassUID), localCopy.OutgoingUid);
            Assert.NotEqual(originalStudyUid, dataset.GetString(DicomTag.StudyInstanceUID));
        }

        [Fact]
        public async Task ExternalAppPlugin_Should_Save_NewValues()
        {
            var toolkit = new Mock<IDicomToolkit>();

            RemoteAppExecution localCopy = new RemoteAppExecution();

            _repository.Setup(r => r.AddAsync(It.IsAny<RemoteAppExecution>(), It.IsAny<CancellationToken>()))
                .Callback((RemoteAppExecution item, CancellationToken c) =>
                localCopy = item
                );

            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.AccessionNumber, "AccesssionNumber" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var dicomFile = new DicomFile(dataset);

            var originalStudyUid = dataset.GetString(DicomTag.StudyInstanceUID);

            toolkit.Setup(t => t.Load(It.IsAny<byte[]>())).Returns(dicomFile);

            var pluginEngine = new OutputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<OutputDataPluginEngine>>().Object,
                toolkit.Object);
            pluginEngine.Configure(new List<string>() { typeof(ExternalAppOutgoing).AssemblyQualifiedName });

            string[] destinations = { "fred" };

            var exportMessage = new ExportRequestDataMessage(new Messaging.Events.ExportRequestEvent() { Destinations = destinations }, "");

            var exportRequestDataMessage = await pluginEngine.ExecutePlugins(exportMessage);

            Assert.Equal(localCopy.ProxyValues[DicomTag.StudyInstanceUID.ToString()], dataset.GetString(DicomTag.StudyInstanceUID));
        }

        [Fact]
        public async Task ExternalAppPlugin_Should_Repare_StudyUid()
        {
            var toolkit = new Mock<IDicomToolkit>();

            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var dicomFile = new DicomFile(dataset);
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

            var outboundStudyUid = dataset.GetString(DicomTag.StudyInstanceUID);
            var originalStudyUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;

            var remoteAppExecution = new RemoteAppExecution
            {
                OutgoingUid = outboundStudyUid,
                StudyUid = originalStudyUid,
                OriginalValues = { { DicomTag.StudyInstanceUID.ToString(), originalStudyUid } }
            };

            _repository.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(remoteAppExecution));

            var pluginEngine = new InputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<InputDataPluginEngine>>().Object);

            pluginEngine.Configure(new List<string>() { typeof(ExternalAppIncoming).AssemblyQualifiedName });

            var (resultDicomFile, resultDicomInfo) = await pluginEngine.ExecutePlugins(dicomFile, dicomInfo);

            Assert.Equal(originalStudyUid, resultDicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
            Assert.NotEqual(outboundStudyUid, resultDicomFile.Dataset.GetString(DicomTag.StudyInstanceUID));
        }

        [Fact]
        public async Task ExternalAppPlugin_Should_Set_WorkflowIds()
        {

            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var dicomFile = new DicomFile(dataset);
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

            var workflowInstanceId = "some guid here";
            var workflowTaskId = "some guid here 2";

            var remoteAppExecution = new RemoteAppExecution
            {
                OutgoingUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                StudyUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                WorkflowInstanceId = workflowInstanceId,
                ExportTaskId = workflowTaskId
            };

            _repository.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(remoteAppExecution));

            var pluginEngine = new InputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<InputDataPluginEngine>>().Object);

            pluginEngine.Configure(new List<string>() { typeof(ExternalAppIncoming).AssemblyQualifiedName });

            var (resultDicomFile, resultDicomInfo) = await pluginEngine.ExecutePlugins(dicomFile, dicomInfo);
            Assert.Equal(workflowInstanceId, resultDicomInfo.WorkflowInstanceId);
            Assert.Equal(workflowTaskId, resultDicomInfo.TaskId);
        }

        [Fact]
        public async Task ExternalAppPlugin_Should_GetData_BasedOnConfig_Tag()
        {
            var sOPClassUID = DicomUID.SecondaryCaptureImageStorage.UID;

            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, sOPClassUID }
            };
            var dicomFile = new DicomFile(dataset);
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

            var workflowInstanceId = "some guid here";
            var workflowTaskId = "some guid here 2";

            var remoteAppExecution = new RemoteAppExecution
            {
                OutgoingUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                StudyUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID,
                WorkflowInstanceId = workflowInstanceId,
                ExportTaskId = workflowTaskId
            };

            _repository.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(remoteAppExecution));

            var pluginEngine = new InputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<InputDataPluginEngine>>().Object);

            pluginEngine.Configure(new List<string>() { typeof(ExternalAppIncoming).AssemblyQualifiedName });

            var (resultDicomFile, resultDicomInfo) = await pluginEngine.ExecutePlugins(dicomFile, dicomInfo);

            _repository.Verify(r => r.GetAsync(sOPClassUID, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task ExternalAppPlugin_Should_Reuse_Data_To_Group()
        {
            var sOPClassUID = DicomUID.SecondaryCaptureImageStorage.UID;
            var toolkit = new Mock<IDicomToolkit>();

            RemoteAppExecution localCopy = new RemoteAppExecution();

            _repository.Setup(r => r.AddAsync(It.IsAny<RemoteAppExecution>(), It.IsAny<CancellationToken>()))
                .Callback((RemoteAppExecution item, CancellationToken c) =>
                localCopy = item
                );

            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, sOPClassUID }
            };
            var dicomFile = new DicomFile(dataset);

            toolkit.Setup(t => t.Load(It.IsAny<byte[]>())).Returns(dicomFile);

            var pluginEngine = new OutputDataPluginEngine(
                _serviceProvider,
                new Mock<ILogger<OutputDataPluginEngine>>().Object,
                toolkit.Object);
            pluginEngine.Configure(new List<string>() { typeof(ExternalAppOutgoing).AssemblyQualifiedName });

            string[] destinations = { "fred" };

            var exportMessage = new ExportRequestDataMessage(new Messaging.Events.ExportRequestEvent() { Destinations = destinations }, "");

            await pluginEngine.ExecutePlugins(exportMessage);
            var firstValue = dataset.GetString(DicomTag.SOPClassUID);

            _repository.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(localCopy));

            await pluginEngine.ExecutePlugins(exportMessage);

            Assert.NotEqual(dataset.GetString(DicomTag.SOPClassUID), sOPClassUID);
            Assert.Equal(firstValue, dataset.GetString(DicomTag.SOPClassUID));
        }
    }
}
