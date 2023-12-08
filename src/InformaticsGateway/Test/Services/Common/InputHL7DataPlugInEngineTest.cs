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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Test.PlugIns;
using Monai.Deploy.Messaging.Events;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class InputHL7DataPlugInEngineTest
    {
        private readonly Mock<ILogger<InputHL7DataPlugInEngine>> _logger;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly ServiceProvider _serviceProvider;

        private const string SampleMessage = "MSH|^~\\&|MD|MD HOSPITAL|MD Test|MONAI Deploy|202207130000|SECURITY|MD^A01^ADT_A01|MSG00001|P|2.8|||<ACK>|\r\n";

        public InputHL7DataPlugInEngineTest()
        {
            _logger = new Mock<ILogger<InputHL7DataPlugInEngine>>();
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
        public void GivenAnInputHL7DataPlugInEngine_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new InputHL7DataPlugInEngine(null, null));
            Assert.Throws<ArgumentNullException>(() => new InputHL7DataPlugInEngine(_serviceProvider, null));

            _ = new InputHL7DataPlugInEngine(_serviceProvider, _logger.Object);
        }


        [Fact]
        public void GivenAnInputHL7DataPlugInEngine_WhenConfigureIsCalledWithBogusAssemblies_ThrowsException()
        {
            var pluginEngine = new InputHL7DataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { "SomeBogusAssemblye" };

            var exceptions = Assert.Throws<AggregateException>(() => pluginEngine.Configure(assemblies));

            Assert.Single(exceptions.InnerExceptions);
            Assert.True(exceptions.InnerException is PlugInLoadingException);
            Assert.Contains("Error loading plug-in 'SomeBogusAssemblye'", exceptions.InnerException.Message);
        }

        [Fact]
        public void GivenAnInputHL7DataPlugInEngine_WhenConfigureIsCalledWithAValidAssembly_ExpectNoExceptions()
        {
            var pluginEngine = new InputHL7DataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { typeof(TestInputHL7DataPlugInAddWorkflow).AssemblyQualifiedName };

            pluginEngine.Configure(assemblies);
            Assert.NotNull(pluginEngine);
        }

        [Fact]
        public async Task GivenAnInputHL7DataPlugInEngine_WhenExecutePlugInsIsCalledWithoutConfigure_ThrowsException()
        {
            var pluginEngine = new InputHL7DataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>() { typeof(TestInputHL7DataPlugInAddWorkflow).AssemblyQualifiedName };

            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                "StudyInstanceUID",
                "SeriesInstanceUID",
                "SOPInstanceUID",
                DataService.DicomWeb,
                "calling",
                "called");

            var message = new HL7.Dotnetcore.Message(SampleMessage);
            message.ParseMessage();

            await Assert.ThrowsAsync<PlugInInitializationException>(async () => await pluginEngine.ExecutePlugInsAsync(message, dicomInfo, null));
        }

        [Fact]
        public async Task GivenAnInputHL7DataPlugInEngine_WhenExecutePlugInsIsCalled_ExpectDataIsProcessedByPlugInAsync()
        {
            var pluginEngine = new InputHL7DataPlugInEngine(_serviceProvider, _logger.Object);
            var assemblies = new List<string>()
            {
                typeof(TestInputHL7DataPlugInAddWorkflow).AssemblyQualifiedName,
            };

            pluginEngine.Configure(assemblies);

            var dicomInfo = new DicomFileStorageMetadata(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                "StudyInstanceUID",
                "SeriesInstanceUID",
                "SOPInstanceUID",
                DataService.DicomWeb,
                "calling",
                "called");

            var message = new HL7.Dotnetcore.Message(SampleMessage);
            message.ParseMessage();
            var configItem = new Hl7ApplicationConfigEntity { PlugInAssemblies = new List<string> { { "TestInputHL7DataPlugInAddWorkflow" } } };

            var (Hl7Message, resultDicomInfo) = await pluginEngine.ExecutePlugInsAsync(message, dicomInfo, configItem);

            Assert.Equal(Hl7Message, message);
            Assert.Equal(resultDicomInfo, dicomInfo);
            Assert.Equal(Hl7Message.GetValue("MSH.3"), TestInputHL7DataPlugInAddWorkflow.TestString);
        }
    }
}
