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
        // public virtual DbSet<InferenceRequest> InferenceRequests { get; set; }
        // public virtual DbSet<InferenceJob> InferenceJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new MonaiApplicationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new SourceApplicationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new DestinationApplicationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new FileStorageInfoConfiguration());
            modelBuilder.ApplyConfiguration(new InferenceRequestConfiguration());
        }
    }
}