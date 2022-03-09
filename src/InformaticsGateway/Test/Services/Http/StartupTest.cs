// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class StartupTest
    {
        [Fact(DisplayName = "Startup - Web host startup builds properly")]
        public async Task Startup_WebHostBuildsProperly()
        {
            var webHost = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder()
                .UseEnvironment("Development")
                .UseStartup<Startup>()
                .Build();
            Assert.NotNull(webHost);

            _ = webHost.RunAsync();

            await webHost.StopAsync();
        }
    }
}
