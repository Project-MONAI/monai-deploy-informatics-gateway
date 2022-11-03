/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2021 NVIDIA Corporation
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
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.InformaticsGateway.Database.Sqlite
{
    /// <summary>
    /// Used to EF migration.
    /// </summary>
    public class InformaticsGatewayContextFactory : IDesignTimeDbContextFactory<InformaticsGatewayContext>
    {
        public InformaticsGatewayContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<InformaticsGatewayContext>();

            var connectionString = configuration.GetConnectionString(Configurations.SR.DatabaseConnectionStringKey);
            builder.UseSqlite(connectionString);

            return new InformaticsGatewayContext(builder.Options);
        }
    }
}
