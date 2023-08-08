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
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Test.Plugins;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class OutputDataPluginEngineTest
    {
        private readonly Mock<ILogger<OutputDataPluginEngine>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly ServiceProvider _serviceProvider;

        public OutputDataPluginEngineTest()
        {
            _logger = new Mock<ILogger<OutputDataPluginEngine>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _dicomToolkit = new DicomToolkit();

            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => _dicomToolkit);

            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void GivenAnOutputDataPluginEngine_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new OutputDataPluginEngine(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new OutputDataPluginEngine(_serviceProvider, null, null));
            Assert.Throws<ArgumentNullException>(() => new OutputDataPluginEngine(_serviceProvider, _logger.Object, null));

            _ = new OutputDataPluginEngine(_serviceProvider, _logger.Object, _dicomToolkit);
        }

        [Fact]
        public void GivenAnOutputDataPluginEngine_WhenConfigureIsCalledWithBogusAssemblies_ThrowsException()
        {
            var pluginEngine = new OutputDataPluginEngine(_serviceProvider, _logger.Object, _dicomToolkit);
            var assemblies = new List<string>() { "SomeBogusAssemblye" };

            var exceptions = Assert.Throws<AggregateException>(() => pluginEngine.Configure(assemblies));

            Assert.Single(exceptions.InnerExceptions);
            Assert.True(exceptions.InnerException is PlugingLoadingException);
            Assert.Contains("Error loading plug-in 'SomeBogusAssemblye'", exceptions.InnerException.Message);
        }

        [Fact]
        public void GivenAnOutputDataPluginEngine_WhenConfigureIsCalledWithAValidAssembly_ExpectNoExceptions()
        {
            var pluginEngine = new OutputDataPluginEngine(_serviceProvider, _logger.Object, _dicomToolkit);
            var assemblies = new List<string>() { typeof(TestOutputDataPluginAddMessage).AssemblyQualifiedName };

            pluginEngine.Configure(assemblies);
        }

        [Fact]
        public async Task GivenAnOutputDataPluginEngine_WhenExecutePluginsIsCalledWithoutConfigure_ThrowsException()
        {
            var pluginEngine = new OutputDataPluginEngine(_serviceProvider, _logger.Object, _dicomToolkit);
            var assemblies = new List<string>() { typeof(TestOutputDataPluginAddMessage).AssemblyQualifiedName };

            var message = new ExportRequestDataMessage(new Messaging.Events.ExportRequestEvent
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            }, "filename.dcm");

            await Assert.ThrowsAsync<ApplicationException>(async () => await pluginEngine.ExecutePlugins(message));
        }

        [Fact]
        public async Task GivenAnOutputDataPluginEngine_WhenExecutePluginsIsCalled_ExpectDataIsProcessedByPluginAsync()
        {
            var pluginEngine = new OutputDataPluginEngine(_serviceProvider, _logger.Object, _dicomToolkit);
            var assemblies = new List<string>()
            {
                typeof(TestOutputDataPluginAddMessage).AssemblyQualifiedName,
                typeof(TestOutputDataPluginModifyDicomFile).AssemblyQualifiedName
            };

            pluginEngine.Configure(assemblies);

            var dicomFile = GenerateDicomFile();
            var message = new ExportRequestDataMessage(new Messaging.Events.ExportRequestEvent
            {
                CorrelationId = Guid.NewGuid().ToString(),
                MessageId = Guid.NewGuid().ToString(),
                WorkflowInstanceId = Guid.NewGuid().ToString(),
            }, "filename.dcm");
            using var ms = new MemoryStream();
            await dicomFile.SaveAsync(ms);
            message.SetData(ms.ToArray());

            var resultMessage = await pluginEngine.ExecutePlugins(message);
            using var resultMs = new MemoryStream(resultMessage.FileContent);
            var resultDicomFile = await DicomFile.OpenAsync(resultMs);

            Assert.Equal(resultMessage, message);
            Assert.True(resultMessage.Messages.Contains(TestOutputDataPluginAddMessage.ExpectedValue));
            Assert.Equal(TestOutputDataPluginModifyDicomFile.ExpectedValue, resultDicomFile.Dataset.GetString(TestOutputDataPluginModifyDicomFile.ExpectedTag));
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
