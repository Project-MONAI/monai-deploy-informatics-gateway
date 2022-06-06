// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client;
using Monai.Deploy.InformaticsGateway.DicomWeb.Client.API;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.CLI
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // target T as ConsoleAppBase.
            await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddSimpleConsole(configure =>
                    {
                        configure.IncludeScopes = false;
                        configure.TimestampFormat = "hh:mm:ss ";
                    });

                    // Configure MinimumLogLevel(CreaterDefaultBuilder's default is Warning).
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient<IDicomWebClient, DicomWebClient>(configure => configure.Timeout = TimeSpan.FromMinutes(60))
                        .SetHandlerLifetime(TimeSpan.FromMinutes(60));
                })
                .RunConsoleAppFrameworkAsync(args).ConfigureAwait(false);
        }
    }
}
