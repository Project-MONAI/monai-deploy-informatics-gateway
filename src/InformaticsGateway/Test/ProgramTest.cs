// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test
{
    public class ProgramTest
    {
        [Fact(DisplayName = "Program - runs properly")]
        public async Task Startup_RunsProperly()
        {
            var host = Program.CreateHostBuilder(System.Array.Empty<string>()).Build();

            Assert.NotNull(host);

            Program.InitializeDatabase(host);

            _ = host.StartAsync();

            await host.StopAsync();
        }
    }
}
