/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.IO.Abstractions;
using System.Reflection;
using FellowOakDicom.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Repositories;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Monai.Deploy.InformaticsGateway.Services.Export;
using Monai.Deploy.InformaticsGateway.Services.Fhir;
using Monai.Deploy.InformaticsGateway.Services.HealthLevel7;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Scu;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.Messaging;
using Monai.Deploy.Messaging.Configuration;
using Monai.Deploy.Security.Authentication.Configurations;
using Monai.Deploy.Storage;
using Monai.Deploy.Storage.Configuration;
using NLog;
using NLog.LayoutRenderers;
using NLog.Web;
using LogManager = NLog.LogManager;

namespace Monai.Deploy.InformaticsGateway
{
    internal class Program
    {
        protected Program()
        { }

        private static void Main(string[] args)
        {
            var version = typeof(Program).Assembly;
            var assemblyVersionNumber = version.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.1";

            var logger = ConfigureNLog(assemblyVersionNumber);
            logger.Info($"Initializing MONAI Deploy Informatics Gateway v{assemblyVersionNumber}");

            var host = CreateHostBuilder(args).Build();
            host.MigrateDatabase();
            host.Run();
            logger.Info("MONAI Deploy Informatics Gateway shutting down.");
        }

        internal static IHostBuilder CreateHostBuilder(string[] args) =>
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
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_TEST")}.json", optional: true, reloadOnChange: false)
                        .AddEnvironmentVariables();
                })
                .ConfigureLogging((builderContext, builder) =>
                {
                    builder.ClearProviders();
                    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<InformaticsGatewayConfiguration>().Bind(hostContext.Configuration.GetSection("InformaticsGateway"));
                    services.AddOptions<MessageBrokerServiceConfiguration>().Bind(hostContext.Configuration.GetSection("InformaticsGateway:messaging"));
                    services.AddOptions<StorageServiceConfiguration>().Bind(hostContext.Configuration.GetSection("InformaticsGateway:storage"));
                    services.AddOptions<AuthenticationOptions>().Bind(hostContext.Configuration.GetSection("MonaiDeployAuthentication"));
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InformaticsGatewayConfiguration>, ConfigurationValidator>());

                    services.ConfigureDatabase(hostContext.Configuration?.GetSection("ConnectionStrings"));

                    services.AddTransient<IFileSystem, FileSystem>();
                    services.AddTransient<IDicomToolkit, DicomToolkit>();
                    services.AddTransient<IStowService, StowService>();
                    services.AddTransient<IFhirService, FhirService>();
                    services.AddTransient<IStreamsWriter, StreamsWriter>();
                    services.AddTransient<IApplicationEntityHandler, ApplicationEntityHandler>();

                    services.AddScoped<IPayloadMoveActionHandler, PayloadMoveActionHandler>();
                    services.AddScoped<IPayloadNotificationActionHandler, PayloadNotificationActionHandler>();

                    services.AddMonaiDeployStorageService(hostContext.Configuration.GetSection("InformaticsGateway:storage:serviceAssemblyName").Value, Monai.Deploy.Storage.HealthCheckOptions.ServiceHealthCheck);

                    services.AddMonaiDeployMessageBrokerPublisherService(hostContext.Configuration.GetSection("InformaticsGateway:messaging:publisherServiceAssemblyName").Value, true);
                    services.AddMonaiDeployMessageBrokerSubscriberService(hostContext.Configuration.GetSection("InformaticsGateway:messaging:subscriberServiceAssemblyName").Value, true);

                    services.AddSingleton<ConfigurationValidator>();
                    services.AddSingleton<IObjectUploadQueue, ObjectUploadQueue>();
                    services.AddSingleton<IPayloadAssembler, PayloadAssembler>();
                    services.AddSingleton<FellowOakDicom.Log.ILogManager, NLogManager>();
                    services.AddSingleton<IMonaiServiceLocator, MonaiServiceLocator>();
                    services.AddSingleton<IStorageInfoProvider, StorageInfoProvider>();
                    services.AddSingleton<IMonaiAeChangedNotificationService, MonaiAeChangedNotificationService>();
                    services.AddSingleton<ITcpListenerFactory, TcpListenerFactory>();
                    services.AddSingleton<IMllpClientFactory, MllpClientFactory>();
                    services.AddSingleton<IApplicationEntityManager, ApplicationEntityManager>();
                    services.AddSingleton<IObjectUploadQueue, ObjectUploadQueue>();
                    services.AddSingleton<IScuQueue, ScuQueue>();
                    services.AddSingleton<ScpService>();
                    services.AddSingleton<ScuService>();
                    services.AddSingleton<ScuExportService>();
                    services.AddSingleton<DicomWebExportService>();
                    services.AddSingleton<DataRetrievalService>();
                    services.AddSingleton<PayloadNotificationService>();
                    services.AddSingleton<MllpService>();
                    services.AddSingleton<ObjectUploadService>();

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

                    services.AddHostedService<ObjectUploadService>(p => p.GetService<ObjectUploadService>());
                    services.AddHostedService<DataRetrievalService>(p => p.GetService<DataRetrievalService>());
                    services.AddHostedService<ScpService>(p => p.GetService<ScpService>());
                    services.AddHostedService<ScuService>(p => p.GetService<ScuService>());
                    services.AddHostedService<ScuExportService>(p => p.GetService<ScuExportService>());
                    services.AddHostedService<DicomWebExportService>(p => p.GetService<DicomWebExportService>());
                    services.AddHostedService<PayloadNotificationService>(p => p.GetService<PayloadNotificationService>());
                    services.AddHostedService<MllpService>(p => p.GetService<MllpService>());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = int.MaxValue);
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseStartup<Startup>();
                })
                .UseNLog();

        private static NLog.Logger ConfigureNLog(string assemblyVersionNumber)
        {
            LayoutRenderer.Register("servicename", logEvent => typeof(Program).Namespace);
            LayoutRenderer.Register("serviceversion", logEvent => assemblyVersionNumber);
            LayoutRenderer.Register("machinename", logEvent => Environment.MachineName);

            return LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
        }
    }
}
