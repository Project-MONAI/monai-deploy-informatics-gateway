﻿/*
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
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.InformaticsGateway.Test.PlugIns;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class OutputDataPlugInEngineFactoryTest
    {
        private readonly Mock<ILogger<OutputDataPlugInEngineFactory>> _logger;
        private readonly FileSystem _fileSystem;
        private readonly ITestOutputHelper _output;

        public OutputDataPlugInEngineFactoryTest(ITestOutputHelper output)
        {
            _logger = new Mock<ILogger<OutputDataPlugInEngineFactory>>();
            _fileSystem = new FileSystem();
            _output = output;
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        }

        [Fact]
        public void RegisteredPlugIns_WhenCalled_ReturnsListOfPlugIns()
        {
            var factory = new OutputDataPlugInEngineFactory(_fileSystem, _logger.Object);

            var result = factory.RegisteredPlugIns().OrderBy(r => r.Value).ToArray();

            _output.WriteLine($"result now = {JsonSerializer.Serialize(result)}");

            Assert.Equal(3, result.Length);

            Assert.Equal(typeof(DicomDeidentifier).GetCustomAttribute<PlugInNameAttribute>().Name, result[0].Key);
            Assert.Equal(typeof(TestOutputDataPlugInAddMessage).GetCustomAttribute<PlugInNameAttribute>().Name, result[1].Key);
            Assert.Equal(typeof(TestOutputDataPlugInModifyDicomFile).GetCustomAttribute<PlugInNameAttribute>().Name, result[2].Key);


            Assert.Collection(result,
                p => VerifyPlugIn(p, typeof(DicomDeidentifier)),
                p => VerifyPlugIn(p, typeof(TestOutputDataPlugInAddMessage)),
                p => VerifyPlugIn(p, typeof(TestOutputDataPlugInModifyDicomFile))
                );

            _logger.VerifyLogging($"{typeof(IOutputDataPlugIn).Name} data plug-in found {typeof(TestOutputDataPlugInAddMessage).GetCustomAttribute<PlugInNameAttribute>()?.Name}: {typeof(TestOutputDataPlugInAddMessage).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IOutputDataPlugIn).Name} data plug-in found {typeof(TestOutputDataPlugInModifyDicomFile).GetCustomAttribute<PlugInNameAttribute>()?.Name}: {typeof(TestOutputDataPlugInModifyDicomFile).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IOutputDataPlugIn).Name} data plug-in found {typeof(DicomDeidentifier).GetCustomAttribute<PlugInNameAttribute>()?.Name}: {typeof(DicomDeidentifier).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
        }

        private void VerifyPlugIn(KeyValuePair<string, string> values, Type type)
        {
            Assert.Equal(values.Key, type.GetCustomAttribute<PlugInNameAttribute>()?.Name);
            Assert.Equal(values.Value, type.GetShortTypeAssemblyName());
        }
    }
}
