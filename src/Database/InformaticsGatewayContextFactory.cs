// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Database
{
    /// <summary>
    /// Used to EF migration.
    /// </summary>
    public class InformaticsGatewayContextFactory : IDesignTimeDbContextFactory<InformaticsGatewayContext>
    {
        public InformaticsGatewayContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<InformaticsGatewayContext>();

            var connectionString = configuration.GetConnectionString(InformaticsGatewayConfiguration.DatabaseConnectionStringKey);
            builder.UseSqlite(connectionString);

            return new InformaticsGatewayContext(builder.Options);
        }
    }
}
