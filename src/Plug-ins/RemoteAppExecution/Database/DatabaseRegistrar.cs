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
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Database.Api;

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database
{
    public class DatabaseRegistrar : DatabaseRegistrationBase
    {
        public override IServiceCollection Configure(IServiceCollection services, DatabaseType databaseType, string? connectionString)
        {
            Guard.Against.Null(services, nameof(services));

            switch (databaseType)
            {
                case DatabaseType.EntityFramework:
                    Guard.Against.Null(connectionString, nameof(connectionString));
                    services.AddDbContext<EntityFramework.RemoteAppExecutionDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Transient);
                    services.AddScoped<IDatabaseMigrationManagerForPlugIns, EntityFramework.MigrationManager>();
                    services.AddScoped(typeof(IRemoteAppExecutionRepository), typeof(EntityFramework.RemoteAppExecutionRepository));
                    break;

                case DatabaseType.MongoDb:
                    services.AddScoped<IDatabaseMigrationManagerForPlugIns, MongoDb.MigrationManager>();
                    services.AddScoped(typeof(IRemoteAppExecutionRepository), typeof(MongoDb.RemoteAppExecutionRepository));
                    break;
            }

            return services;
        }
    }
}
