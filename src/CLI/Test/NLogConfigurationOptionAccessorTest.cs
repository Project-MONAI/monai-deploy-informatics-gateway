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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class NLogConfigurationOptionAccessorTest
    {
        public NLogConfigurationOptionAccessorTest()
        {
        }

        [Fact(DisplayName = "NLogConfigurationOptionAccessor Constructor")]
        public void DockerRunner_Constructor()
        {
            Assert.Throws<ArgumentNullException>(() => new NLogConfigurationOptionAccessor(null));
        }


        [Fact]
        public void DicomListeningPort_Get_ReturnsValue()
        {
            var fileSystem = SetupFileSystem();
            var configurationOptionAccessor = new NLogConfigurationOptionAccessor(fileSystem);

            Assert.Equal($"{Common.ContainerApplicationRootPath}/logs/", configurationOptionAccessor.LogStoragePath);
        }

        private IFileSystem SetupFileSystem()
        {
            return new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {Common.NLogConfigFilePath, new MockFileData("<nlog xmlns=\"http://www.nlog-project.org/schemas/NLog.xsd\">\r\n  <variable name=\"logDir\" value=\"${basedir}/logs/\" />\r\n</nlog>") }
            });
        }
    }
}
