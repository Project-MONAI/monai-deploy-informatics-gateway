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
using Monai.Deploy.InformaticsGateway.Test.PlugIns;
using Monai.Deploy.Messaging.Events;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class InputDataPlugInEngineTest
    {
        private readonly Mock<ILogger<InputDataPlugInEngine>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        public InputDataPlugInEngineTest()
        {
            _logger = new Mock<ILogger<InputDataPlugInEngine>>();
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
        public void GivenAnInputDataPlugInEngine_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new InputDataPlugInEngine(null, null));
            Assert.Throws<ArgumentNullException>(() => new InputDataPlugInEngine(_serviceProvider, null));

            _ = new InputDataPlugInEngine(_serviceProvider, _logger.Object);
        }

        [Fact]
        public void GivenAnInputDataPlugInEngine_WhenConfigureIsCalledWithBogusAssemblies_ThrowsException()
        {
            var pluginEngine = new InputDataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { "SomeBogusAssemblye" };

            var exceptions = Assert.Throws<AggregateException>(() => pluginEngine.Configure(assemblies));

            Assert.Single(exceptions.InnerExceptions);
            Assert.True(exceptions.InnerException is PlugInLoadingException);
            Assert.Contains("Error loading plug-in 'SomeBogusAssemblye'", exceptions.InnerException.Message);
        }

        [Fact]
        public void GivenAnInputDataPlugInEngine_WhenConfigureIsCalledWithAValidAssembly_ExpectNoExceptions()
        {
            var pluginEngine = new InputDataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { typeof(TestInputDataPlugInAddWorkflow).AssemblyQualifiedName };

            pluginEngine.Configure(assemblies);
        }

        [Fact]
        public async Task GivenAnInputDataPlugInEngine_WhenExecutePlugInsIsCalledWithoutConfigure_ThrowsException()
        {
            var pluginEngine = new InputDataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { typeof(TestInputDataPlugInAddWorkflow).AssemblyQualifiedName };

            var dicomFile = GenerateDicomFile();
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
                DataService.DicomWeb,
                "calling",
                "called");

            await Assert.ThrowsAsync<ApplicationException>(async () => await pluginEngine.ExecutePlugInsAsync(dicomFile, dicomInfo));
        }

        [Fact]
        public async Task GivenAnInputDataPlugInEngine_WhenExecutePlugInsIsCalled_ExpectDataIsProcessedByPlugInAsync()
        {
            var pluginEngine = new InputDataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>()
            {
                typeof(TestInputDataPlugInAddWorkflow).AssemblyQualifiedName,
                typeof(TestInputDataPlugInModifyDicomFile).AssemblyQualifiedName,
                typeof(TestInputDataPlugInResumeWorkflow).AssemblyQualifiedName,
            };

            pluginEngine.Configure(assemblies);

            var dicomFile = GenerateDicomFile();
            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SeriesInstanceUID),
                dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID),
                DataService.DicomWeb,
                "calling",
                "called");

            var (resultDicomFile, resultDicomInfo) = await pluginEngine.ExecutePlugInsAsync(dicomFile, dicomInfo);

            Assert.Equal(resultDicomFile, dicomFile);
            Assert.Equal(resultDicomInfo, dicomInfo);
            Assert.True(dicomInfo.Workflows.Contains(TestInputDataPlugInAddWorkflow.TestString));
            Assert.Equal(TestInputDataPlugInModifyDicomFile.ExpectedValue, resultDicomFile.Dataset.GetString(TestInputDataPlugInModifyDicomFile.ExpectedTag));

            Assert.Equal(resultDicomInfo.WorkflowInstanceId, TestInputDataPlugInResumeWorkflow.WorkflowInstanceId);
            Assert.Equal(resultDicomInfo.TaskId, TestInputDataPlugInResumeWorkflow.TaskId);
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