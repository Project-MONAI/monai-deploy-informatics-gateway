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
using Monai.Deploy.InformaticsGateway.Database.Sqlite;
using Monai.Deploy.InformaticsGateway.Database.Sqlite.Configurations;

namespace Monai.Deploy.InformaticsGateway.Database
{
    public static class DatabaseManager
    {
        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfigurationSection? connectionStringConfigurationSection)
        {
            if (connectionStringConfigurationSection is null)
            {
                throw new ConfigurationException("No database connections found in configuration section 'ConnectionStrings'.");
            }


            var databaseType = connectionStringConfigurationSection["Type"];
            switch (databaseType)
            {
                case "Sqlite":
                    services.AddScoped<IDatabaseMigrationManager, SqliteDatabaseMigrationManager>();
                    services.AddDbContext<InformaticsGatewayContext>(
                        options => options.UseSqlite(connectionStringConfigurationSection[SR.DatabaseConnectionStringKey]),
                        ServiceLifetime.Transient);
                    return services;
                default:
                    throw new ConfigurationException($"Unsupported database type defined: '{databaseType}'");
            }
        }
    }
}
