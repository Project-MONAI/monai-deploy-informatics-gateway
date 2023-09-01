﻿/*
 * Copyright 2022-2023 MONAI Consortium
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
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework;
using Monai.Deploy.InformaticsGateway.Database.MongoDB;
using MongoDB.Driver;
using ConfigurationException = Monai.Deploy.InformaticsGateway.Configuration.ConfigurationException;

namespace Monai.Deploy.InformaticsGateway.Database
{
    public static class DatabaseManager
    {
        public const string DbType_Sqlite = "sqlite";
        public const string DbType_MongoDb = "mongodb";

        public static IHealthChecksBuilder AddDatabaseHealthCheck(this IHealthChecksBuilder healthChecksBuilder, IConfigurationSection? connectionStringConfigurationSection)
        {
            if (connectionStringConfigurationSection is null)
            {
                throw new ConfigurationException("No database connections found in configuration section 'ConnectionStrings'.");
            }

            var databaseType = connectionStringConfigurationSection["Type"].ToLowerInvariant();

            switch (databaseType)
            {
                case DbType_Sqlite:
                    healthChecksBuilder.AddDbContextCheck<InformaticsGatewayContext>("SQLite Database");
                    return healthChecksBuilder;

                case DbType_MongoDb:
                    healthChecksBuilder.AddMongoDb(mongodbConnectionString: connectionStringConfigurationSection[SR.DatabaseConnectionStringKey], mongoDatabaseName: connectionStringConfigurationSection[SR.DatabaseNameKey], name: "MongoDB");
                    return healthChecksBuilder;

                default:
                    throw new ConfigurationException($"Unsupported database type defined: '{databaseType}'");
            }
        }

        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfigurationSection? connectionStringConfigurationSection)
            => services.ConfigureDatabase(connectionStringConfigurationSection, new FileSystem());

        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfigurationSection? connectionStringConfigurationSection, IFileSystem fileSystem)
        {
            if (connectionStringConfigurationSection is null)
            {
                throw new ConfigurationException("No database connections found in configuration section 'ConnectionStrings'.");
            }

            var databaseType = connectionStringConfigurationSection["Type"].ToLowerInvariant();
            switch (databaseType)
            {
                case DbType_Sqlite:
                    services.AddDbContext<InformaticsGatewayContext>(
                        options => options.UseSqlite(connectionStringConfigurationSection[SR.DatabaseConnectionStringKey]),
                        ServiceLifetime.Transient);
                    services.AddScoped<IDatabaseMigrationManager, EfDatabaseMigrationManager>();
                    services.AddScoped(typeof(IDestinationApplicationEntityRepository), typeof(EntityFramework.Repositories.DestinationApplicationEntityRepository));
                    services.AddScoped(typeof(IInferenceRequestRepository), typeof(EntityFramework.Repositories.InferenceRequestRepository));
                    services.AddScoped(typeof(IMonaiApplicationEntityRepository), typeof(EntityFramework.Repositories.MonaiApplicationEntityRepository));
                    services.AddScoped(typeof(ISourceApplicationEntityRepository), typeof(EntityFramework.Repositories.SourceApplicationEntityRepository));
                    services.AddScoped(typeof(IStorageMetadataRepository), typeof(EntityFramework.Repositories.StorageMetadataWrapperRepository));
                    services.AddScoped(typeof(IPayloadRepository), typeof(EntityFramework.Repositories.PayloadRepository));
                    services.AddScoped(typeof(IDicomAssociationInfoRepository), typeof(EntityFramework.Repositories.DicomAssociationInfoRepository));
                    services.AddScoped(typeof(IVirtualApplicationEntityRepository), typeof(EntityFramework.Repositories.VirtualApplicationEntityRepository));

                    services.ConfigureDatabaseFromPlugIns(DatabaseType.EntityFramework, fileSystem, connectionStringConfigurationSection);
                    return services;

                case DbType_MongoDb:
                    services.AddSingleton<IMongoClient, MongoClient>(s => new MongoClient(connectionStringConfigurationSection[SR.DatabaseConnectionStringKey]));
                    services.Configure<DatabaseOptions>(connectionStringConfigurationSection);

                    services.AddScoped<IDatabaseMigrationManager, MongoDatabaseMigrationManager>();
                    services.AddScoped(typeof(IDestinationApplicationEntityRepository), typeof(MongoDB.Repositories.DestinationApplicationEntityRepository));
                    services.AddScoped(typeof(IInferenceRequestRepository), typeof(MongoDB.Repositories.InferenceRequestRepository));
                    services.AddScoped(typeof(IMonaiApplicationEntityRepository), typeof(MongoDB.Repositories.MonaiApplicationEntityRepository));
                    services.AddScoped(typeof(ISourceApplicationEntityRepository), typeof(MongoDB.Repositories.SourceApplicationEntityRepository));
                    services.AddScoped(typeof(IStorageMetadataRepository), typeof(MongoDB.Repositories.StorageMetadataWrapperRepository));
                    services.AddScoped(typeof(IPayloadRepository), typeof(MongoDB.Repositories.PayloadRepository));
                    services.AddScoped(typeof(IDicomAssociationInfoRepository), typeof(MongoDB.Repositories.DicomAssociationInfoRepository));
                    services.AddScoped(typeof(IVirtualApplicationEntityRepository), typeof(MongoDB.Repositories.VirtualApplicationEntityRepository));

                    services.ConfigureDatabaseFromPlugIns(DatabaseType.MongoDb, fileSystem, connectionStringConfigurationSection);

                    return services;

                default:
                    throw new ConfigurationException($"Unsupported database type defined: '{databaseType}'");
            }
        }

        public static IServiceCollection ConfigureDatabaseFromPlugIns(this IServiceCollection services,
            DatabaseType databaseType,
            IFileSystem fileSystem,
            IConfigurationSection? connectionStringConfigurationSection)
        {
            Guard.Against.Null(fileSystem, nameof(fileSystem));

            var assemblies = LoadAssemblyFromPlugInsDirectory(fileSystem);
            var matchingTypes = FindMatchingTypesFromAssemblies(assemblies);

            foreach (var type in matchingTypes)
            {
                if (Activator.CreateInstance(type) is not DatabaseRegistrationBase registrar)
                {
                    throw new ConfigurationException($"Error activating database registration from type '{type.FullName}'.");
                }
                registrar.Configure(services, databaseType, connectionStringConfigurationSection?[SR.DatabaseConnectionStringKey]);
            }
            return services;
        }

        internal static Type[] FindMatchingTypesFromAssemblies(Assembly[] assemblies)
        {
            var matchingTypes = new List<Type>();
            foreach (var assembly in assemblies)
            {
                var types = assembly.ExportedTypes.Where(p => p.IsSubclassOf(typeof(DatabaseRegistrationBase)));
                if (types.Any())
                {
                    matchingTypes.AddRange(types);
                }
            }

            return matchingTypes.ToArray();
        }

        internal static Assembly[] LoadAssemblyFromPlugInsDirectory(IFileSystem fileSystem)
        {
            Guard.Against.Null(fileSystem, nameof(fileSystem));

            if (!fileSystem.Directory.Exists(InformaticsGateway.Api.PlugIns.SR.PlugInDirectoryPath))
            {
                throw new ConfigurationException($"Plug-in directory '{InformaticsGateway.Api.PlugIns.SR.PlugInDirectoryPath}' cannot be found.");
            }

            var assemblies = new List<Assembly>();
            var plugins = fileSystem.Directory.GetFiles(InformaticsGateway.Api.PlugIns.SR.PlugInDirectoryPath, "*.dll");

            foreach (var plugin in plugins)
            {
                var asesmblyeData = fileSystem.File.ReadAllBytes(plugin);
                assemblies.Add(Assembly.Load(asesmblyeData));
            }
            return assemblies.ToArray();
        }
    }
}
