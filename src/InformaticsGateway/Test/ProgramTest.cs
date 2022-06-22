// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test
{
    public class ProgramTest
    {
        const string PlugInDirectoryName = "plug-ins";
        [Fact(DisplayName = "Program - runs properly")]
        public void Startup_RunsProperly()
        {
            var workingDirectory = Environment.CurrentDirectory;
            var plugInDirectory = Path.Combine(workingDirectory, PlugInDirectoryName);
            Directory.CreateDirectory(plugInDirectory);
            var file = Assembly.GetExecutingAssembly().Location;
            File.Copy(file, Path.Combine(plugInDirectory, Path.GetFileName(file)), true);
            var host = Program.CreateHostBuilder(System.Array.Empty<string>()).Build();

            Assert.NotNull(host);

            Program.InitializeDatabase(host);
        }
    }
}
