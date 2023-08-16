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
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.ExecutionPlugins;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.InformaticsGateway.Test.Plugins;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class InputDataPluginEngineFactoryTest
    {
        private readonly Mock<ILogger<InputDataPluginEngineFactory>> _logger;
        private readonly FileSystem _fileSystem;

        public InputDataPluginEngineFactoryTest()
        {
            _logger = new Mock<ILogger<InputDataPluginEngineFactory>>();
            _fileSystem = new FileSystem();

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void RegisteredPlugins_WhenCalled_ReturnsListOfPlugins()
        {
            var factory = new InputDataPluginEngineFactory(_fileSystem, _logger.Object);

            var result = factory.RegisteredPlugins();

            Assert.Collection(result,
                p => VerifyPlugin(p, typeof(TestInputDataPluginAddWorkflow)),
                p => VerifyPlugin(p, typeof(TestInputDataPluginResumeWorkflow)),
                p => VerifyPlugin(p, typeof(TestInputDataPluginModifyDicomFile)),
                p => VerifyPlugin(p, typeof(TestInputDataPluginVirtualAE)),
                p => VerifyPlugin(p, typeof(ExternalAppIncoming)));

            _logger.VerifyLogging($"{typeof(IInputDataPlugin).Name} data plug-in found {typeof(TestInputDataPluginAddWorkflow).GetCustomAttribute<PluginNameAttribute>()?.Name}: {typeof(TestInputDataPluginAddWorkflow).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IInputDataPlugin).Name} data plug-in found {typeof(TestInputDataPluginResumeWorkflow).GetCustomAttribute<PluginNameAttribute>()?.Name}: {typeof(TestInputDataPluginResumeWorkflow).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IInputDataPlugin).Name} data plug-in found {typeof(TestInputDataPluginModifyDicomFile).GetCustomAttribute<PluginNameAttribute>()?.Name}: {typeof(TestInputDataPluginModifyDicomFile).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IInputDataPlugin).Name} data plug-in found {typeof(TestInputDataPluginVirtualAE).GetCustomAttribute<PluginNameAttribute>()?.Name}: {typeof(TestInputDataPluginVirtualAE).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IInputDataPlugin).Name} data plug-in found {typeof(ExternalAppIncoming).GetCustomAttribute<PluginNameAttribute>()?.Name}: {typeof(ExternalAppIncoming).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
        }

        private void VerifyPlugin(KeyValuePair<string, string> values, Type type)
        {
            Assert.Equal(values.Key, type.GetCustomAttribute<PluginNameAttribute>()?.Name);
            Assert.Equal(values.Value, type.GetShortTypeAssemblyName());
        }
    }
}
