/*
 * Copyright 2023 MONAI Consortium
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

using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Database.Api;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database
{
    public class DatabaseRegistrar : DatabaseRegistrationBase
    {
        public override IServiceCollection Configure(
            IServiceCollection services,
            DatabaseType databaseType,
            IConfigurationSection? connectionstringConfigurationSection,
            IConfigurationSection? pluginsConfigurationSection,
            ILoggerFactory loggerFactory)
        {
            Guard.Against.Null(services, nameof(services));
            Guard.Against.Null(connectionstringConfigurationSection, nameof(connectionstringConfigurationSection));

            var logger = loggerFactory.CreateLogger<DatabaseRegistrar>();

            switch (databaseType)
            {
                case DatabaseType.EntityFramework:

                    services.AddDbContext<EntityFramework.RemoteAppExecutionDbContext>(options => options.UseSqlite(connectionstringConfigurationSection[SR.DatabaseConnectionStringKey]), ServiceLifetime.Transient);
                    services.AddScoped<IDatabaseMigrationManagerForPlugIns, EntityFramework.MigrationManager>();
                    logger.AddedDbScope("IDatabaseMigrationManagerForPlugIns", "EntityFramework");
                    services.AddScoped<IRemoteAppExecutionRepository, EntityFramework.RemoteAppExecutionRepository>();
                    logger.AddedDbScope("IRemoteAppExecutionRepository", "EntityFramework");
                    break;

                case DatabaseType.MongoDb:
                    Guard.Against.Null(pluginsConfigurationSection, nameof(pluginsConfigurationSection));
                    services.Configure<DatabaseOptions>(connectionstringConfigurationSection.GetSection("DatabaseOptions"));
                    services.AddScoped<IDatabaseMigrationManagerForPlugIns, MongoDb.MigrationManager>();
                    logger.AddedDbScope("IDatabaseMigrationManagerForPlugIns", "MongoDb");
                    services.AddScoped<IRemoteAppExecutionRepository, MongoDb.RemoteAppExecutionRepository>();
                    logger.AddedDbScope("IRemoteAppExecutionRepository", "MongoDb");
                    break;
            }

            return services;
        }
    }
}
