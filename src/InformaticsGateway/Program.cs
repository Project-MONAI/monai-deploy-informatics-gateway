﻿// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.Storage;
using System;
using System.IO;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            InitializeDatabase(host);
            host.Run();
        }

        private static void InitializeDatabase(IHost host)
        {
            Guard.Against.Null(host, nameof(host));

            using (var serviceScope = host.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<InformaticsGatewayContext>();
                context.Database.Migrate();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var env = builderContext.HostingEnvironment;
                    config
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging((builderContext, configureLogging) =>
                {
                    configureLogging.AddConfiguration(builderContext.Configuration.GetSection("Logging"));
                    configureLogging.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<InformaticsGatewayConfiguration>()
                        .Bind(hostContext.Configuration.GetSection("InformaticsGateway"))
                        .PostConfigure(options =>
                        {
                        });
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InformaticsGatewayConfiguration>, ConfigurationValidator>());

                    services.AddDbContext<InformaticsGatewayContext>(
                        options => options.UseSqlite(hostContext.Configuration.GetConnectionString(InformaticsGatewayConfiguration.DatabaseConnectionStringKey)),
                        ServiceLifetime.Transient);

                    services.AddSingleton<ConfigurationValidator>();
                    services.AddSingleton<IInstanceCleanupQueue, InstanceCleanupQueue>();
                    services.AddSingleton<IPayloadAssembler, PayloadAssembler>();

                    services.AddTransient<IFileSystem, FileSystem>();
                    services.AddTransient<IDicomToolkit, DicomToolkit>();

                    services.AddScoped(typeof(IInformaticsGatewayRepository<>), typeof(InformaticsGatewayRepository<>));

                    services.AddSingleton<MinIoStorageService>();
                    services.AddSingleton<IStorageService>(implementationFactory =>
                    {
                        var options = implementationFactory.GetService<IOptions<InformaticsGatewayConfiguration>>();
                        var serviceProvider = implementationFactory.GetService<IServiceProvider>();
                        var logger = implementationFactory.GetService<ILogger<Program>>();
                        return DynamicServiceLocator.LocateService<IStorageService>(serviceProvider, logger, options.Value.Storage.StorageService);
                    });

                    services.AddSingleton<FellowOakDicom.Log.ILogManager, Logging.FoDicomLogManager>();
                    services.AddSingleton<IMonaiServiceLocator, MonaiServiceLocator>();
                    services.AddSingleton<IStorageInfoProvider, StorageInfoProvider>();
                    services.AddSingleton<IWorkloadManagerApi, WorkloadManagerApi>();
                    services.AddSingleton<IMonaiAeChangedNotificationService, MonaiAeChangedNotificationService>();
                    services.AddSingleton<IApplicationEntityManager, ApplicationEntityManager>();
                    services.AddSingleton<SpaceReclaimerService>();
                    services.AddSingleton<ScpService>();
                    services.AddSingleton<ScuExportService>();
                    services.AddSingleton<DicomWebExportService>();
                    services.AddSingleton<DataRetrievalService>();
                    services.AddSingleton<PayloadNotificationService>();

                    var timeout = TimeSpan.FromSeconds(hostContext.Configuration.GetValue("InformaticsGateway:dicomWeb:clientTimeout", DicomWebConfiguration.DefaultClientTimeout));
                    services
                        .AddHttpClient("dicomweb", configure => configure.Timeout = timeout)
                        .SetHandlerLifetime(timeout);

                    timeout = TimeSpan.FromSeconds(hostContext.Configuration.GetValue("InformaticsGateway:workloadManager:clientTimeout", DicomWebConfiguration.DefaultClientTimeout));
                    services
                        .AddHttpClient("wm", configure => configure.Timeout = timeout)
                        .SetHandlerLifetime(timeout);

                    timeout = TimeSpan.FromSeconds(hostContext.Configuration.GetValue("InformaticsGateway:fhir:clientTimeout", DicomWebConfiguration.DefaultClientTimeout));
                    services
                        .AddHttpClient("fhir", configure => configure.Timeout = timeout)
                        .SetHandlerLifetime(timeout);

                    services.AddHostedService<SpaceReclaimerService>(p => p.GetService<SpaceReclaimerService>());
                    services.AddHostedService<DataRetrievalService>(p => p.GetService<DataRetrievalService>());
                    services.AddHostedService<ScpService>(p => p.GetService<ScpService>());
                    services.AddHostedService<ScuExportService>(p => p.GetService<ScuExportService>());
                    services.AddHostedService<DicomWebExportService>(p => p.GetService<DicomWebExportService>());
                    services.AddHostedService<PayloadNotificationService>(p => p.GetService<PayloadNotificationService>());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseStartup<Startup>();
                });
    }
}
