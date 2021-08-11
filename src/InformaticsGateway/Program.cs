// Copyright 2021 MONAI Consortium
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
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
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
                    services.AddSingleton<IFileStoredNotificationQueue, FileStoredNotificationQueue>();

                    services.AddTransient<IFileSystem, FileSystem>();
                    services.AddTransient<IDicomToolkit, DicomToolkit>();

                    services.AddScoped(typeof(IInformaticsGatewayRepository<>), typeof(InformaticsGatewayRepository<>));

                    services.AddSingleton<IStorageInfoProvider, StorageInfoProvider>();
                    services.AddSingleton<IWorkloadManagerApi, WorkloadManagerApi>();
                    services.AddSingleton<IMonaiAeChangedNotificationService, MonaiAeChangedNotificationService>();
                    services.AddSingleton<IApplicationEntityManager, ApplicationEntityManager>();
                    services.AddSingleton<WorkloadManagerNotificationService>();
                    services.AddSingleton<SpaceReclaimerService>();
                    services.AddSingleton<ScpService>();
                    services.AddSingleton<ScuExportService>();
                    services.AddSingleton<DicomWebExportService>();
                    services.AddSingleton<DataRetrievalService>();

                    var timeout = GetConfigAndConvertToMinutes(hostContext.Configuration, "InformaticsGateway:dicomWeb:clientTimeout", DicomWebConfiguration.DefaultClientTimeout);
                    services
                        .AddHttpClient("dicomweb", configure => configure.Timeout = TimeSpan.FromSeconds(timeout))
                        .SetHandlerLifetime(TimeSpan.FromSeconds(timeout));

                    timeout = GetConfigAndConvertToMinutes(hostContext.Configuration, "InformaticsGateway:workloadManager:clientTimeout", MonaiWorkloadManagerConfiguration.DefaultClientTimeout);
                    services
                        .AddHttpClient("wm", configure => configure.Timeout = TimeSpan.FromSeconds(timeout))
                        .SetHandlerLifetime(TimeSpan.FromSeconds(timeout));

                    timeout = GetConfigAndConvertToMinutes(hostContext.Configuration, "InformaticsGateway:fhir:clientTimeout", FhirConfiguration.DefaultClientTimeout);
                    services
                        .AddHttpClient("fhir", configure => configure.Timeout = TimeSpan.FromSeconds(timeout))
                        .SetHandlerLifetime(TimeSpan.FromSeconds(timeout));

                    services.AddHostedService<WorkloadManagerNotificationService>(p => p.GetService<WorkloadManagerNotificationService>());
                    services.AddHostedService<SpaceReclaimerService>(p => p.GetService<SpaceReclaimerService>());
                    services.AddHostedService<DataRetrievalService>(p => p.GetService<DataRetrievalService>());
                    services.AddHostedService<ScpService>(p => p.GetService<ScpService>());
                    services.AddHostedService<ScuExportService>(p => p.GetService<ScuExportService>());
                    services.AddHostedService<DicomWebExportService>(p => p.GetService<DicomWebExportService>());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseStartup<Startup>();
                });

        private static double GetConfigAndConvertToMinutes(IConfiguration configuration, string key, int defaultValue)
        {
            var configSection = configuration.GetSection(key);
            if (Int32.TryParse(configSection?.Value, out int value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}