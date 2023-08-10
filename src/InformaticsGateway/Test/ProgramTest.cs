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
using System.IO;
using System.Reflection;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test
{
    public class ProgramTest
    {
        private const string PlugInDirectoryName = "plug-ins";

        [RetryFact(maxRetries: 10, delayBetweenRetriesMs: 500, DisplayName = "Program - runs properly")]
        public void Startup_RunsProperly()
        {
            var workingDirectory = Environment.CurrentDirectory;
            var plugInDirectory = Path.Combine(workingDirectory, PlugInDirectoryName);
            Directory.CreateDirectory(plugInDirectory);
            var file = Assembly.GetExecutingAssembly().Location;
            File.Copy(file, Path.Combine(plugInDirectory, Path.GetFileName(file)), true);
            var host = Program.CreateHostBuilder(System.Array.Empty<string>()).Build();

            Assert.NotNull(host);
        }
    }
}
