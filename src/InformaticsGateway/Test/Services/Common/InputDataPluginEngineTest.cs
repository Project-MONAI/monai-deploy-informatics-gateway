/*
 * Copyright 2023 MONAI Consortium
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
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Test.Plugins;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class InputDataPluginEngineTest
    {
        private readonly Mock<ILogger<InputDataPluginEngine>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public InputDataPluginEngineTest()
        {
            _logger = new Mock<ILogger<InputDataPluginEngine>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();

            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenAnInputDataPluginEngine_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new InputDataPluginEngine(null, null));
            Assert.Throws<ArgumentNullException>(() => new InputDataPluginEngine(_serviceProvider, null));

            _ = new InputDataPluginEngine(_serviceProvider, _logger.Object);
        }

        [Fact]
        public void GivenAnInputDataPluginEngine_WhenConfigureIsCalledWithBogusAssemblies_ThrowsException()
        {
            var pluginEngine = new InputDataPluginEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { "SomeBogusAssemblye" };

            var exceptions = Assert.Throws<AggregateException>(() => pluginEngine.Configure(assemblies));

            Assert.Single(exceptions.InnerExceptions);
            Assert.True(exceptions.InnerException is PlugingLoadingException);
            Assert.Contains("Error loading plug-in 'SomeBogusAssemblye'", exceptions.InnerException.Message);
        }

        [Fact]
        public void GivenAnInputDataPluginEngine_WhenConfigureIsCalledWithAValidAssembly_ExpectNoExceptions()
        {
            var pluginEngine = new InputDataPluginEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { typeof(TestInputDataPluginAddWorkflow).AssemblyQualifiedName };

            pluginEngine.Configure(assemblies);
        }

        [Fact]
        public async Task GivenAnInputDataPluginEngine_WhenExecutePluginsIsCalledWithoutConfigure_ThrowsException()
        {
            var pluginEngine = new InputDataPluginEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { typeof(TestInputDataPluginAddWorkflow).AssemblyQualifiedName };

            var dicomFile = GenerateDicomFile();
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

            await Assert.ThrowsAsync<ApplicationException>(async () => await pluginEngine.ExecutePlugins(dicomFile, dicomInfo));
        }

        [Fact]
        public async Task GivenAnInputDataPluginEngine_WhenExecutePluginsIsCalled_ExpectDataIsProcessedByPluginAsync()
        {
            var pluginEngine = new InputDataPluginEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>()
            {
                typeof(TestInputDataPluginAddWorkflow).AssemblyQualifiedName,
                typeof(TestInputDataPluginModifyDicomFile).AssemblyQualifiedName,
                typeof(TestInputDataPluginResumeWorkflow).AssemblyQualifiedName,
            };

            pluginEngine.Configure(assemblies);

            var dicomFile = GenerateDicomFile();
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));

            var (resultDicomFile, resultDicomInfo) = await pluginEngine.ExecutePlugins(dicomFile, dicomInfo);

            Assert.Equal(resultDicomFile, dicomFile);
            Assert.Equal(resultDicomInfo, dicomInfo);
            Assert.True(dicomInfo.Workflows.Contains(TestInputDataPluginAddWorkflow.TestString));
            Assert.Equal(TestInputDataPluginModifyDicomFile.ExpectedValue, resultDicomFile.Dataset.GetString(TestInputDataPluginModifyDicomFile.ExpectedTag));

            Assert.Equal(resultDicomInfo.WorkflowInstanceId, TestInputDataPluginResumeWorkflow.WorkflowInstanceId);
            Assert.Equal(resultDicomInfo.TaskId, TestInputDataPluginResumeWorkflow.TaskId);
        }

        private static DicomFile GenerateDicomFile()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            return new DicomFile(dataset);
        }
    }
}
