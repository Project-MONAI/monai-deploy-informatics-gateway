// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.EntityFrameworkCore;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database
{
    public class InformaticsGatewayContext : DbContext
    {
        public InformaticsGatewayContext(DbContextOptions<InformaticsGatewayContext> options) : base(options)
        {
        }

        public virtual DbSet<MonaiApplicationEntity> MonaiApplicationEntities { get; set; }
        public virtual DbSet<SourceApplicationEntity> SourceApplicationEntities { get; set; }
        public virtual DbSet<DestinationApplicationEntity> DestinationApplicationEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new MonaiApplicationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new SourceApplicationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new DestinationApplicationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new InferenceRequestConfiguration());

            modelBuilder.ApplyConfiguration(new PayloadConfiguration());
        }
    }
}
