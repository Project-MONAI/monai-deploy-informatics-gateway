/*
 * Copyright 2022 MONAI Consortium
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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration;
using Monai.Deploy.InformaticsGateway.Database.MongoDB;
using Monai.Deploy.InformaticsGateway.Database.MongoDB.Configurations;
using MongoDB.Driver;

namespace Monai.Deploy.InformaticsGateway.Database
{
    public static class DatabaseManager
    {
        public const string DbType_Sqlite = "sqlite";
        public const string DbType_MongoDb = "mongodb";
        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfigurationSection? connectionStringConfigurationSection)
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
                    return services;
                case DbType_MongoDb:
                    services.AddSingleton<IMongoClient, MongoClient>(s => new MongoClient(connectionStringConfigurationSection[SR.DatabaseConnectionStringKey]));
                    services.Configure<MongoDBOptions>(connectionStringConfigurationSection);
                    services.AddScoped<IDatabaseMigrationManager, MongoDatabaseMigrationManager>();
                    services.AddScoped(typeof(IDestinationApplicationEntityRepository), typeof(MongoDB.Repositories.DestinationApplicationEntityRepository));
                    services.AddScoped(typeof(IInferenceRequestRepository), typeof(MongoDB.Repositories.InferenceRequestRepository));
                    services.AddScoped(typeof(IMonaiApplicationEntityRepository), typeof(MongoDB.Repositories.MonaiApplicationEntityRepository));
                    services.AddScoped(typeof(ISourceApplicationEntityRepository), typeof(MongoDB.Repositories.SourceApplicationEntityRepository));
                    services.AddScoped(typeof(IStorageMetadataRepository), typeof(MongoDB.Repositories.StorageMetadataWrapperRepository));
                    services.AddScoped(typeof(IPayloadRepository), typeof(MongoDB.Repositories.PayloadRepository));

                    return services;
                default:
                    throw new ConfigurationException($"Unsupported database type defined: '{databaseType}'");
            }
        }
    }
}
