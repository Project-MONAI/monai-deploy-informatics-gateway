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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Database.Sqlite.Configurations;

namespace Monai.Deploy.InformaticsGateway.Database.Sqlite
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
            modelBuilder.ApplyConfiguration(new StorageMetadataWrapperEntityConfiguration());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.ConfigureWarnings(c => c.Log(
                (CoreEventId.SaveChangesCompleted, LogLevel.Trace),
                (CoreEventId.SaveChangesStarting, LogLevel.Trace),
                (CoreEventId.DetectChangesStarting, LogLevel.Trace),
                (CoreEventId.DetectChangesCompleted, LogLevel.Trace),
                (CoreEventId.StartedTracking, LogLevel.Trace),
                (CoreEventId.ContextInitialized, LogLevel.Trace),
                (CoreEventId.StateChanged, LogLevel.Trace),
                (CoreEventId.QueryCompilationStarting, LogLevel.Trace),
                (CoreEventId.QueryExecutionPlanned, LogLevel.Trace),
                (RelationalEventId.CommandExecuting, LogLevel.Trace),
                (RelationalEventId.CommandExecuted, LogLevel.Trace),
                (RelationalEventId.ConnectionClosing, LogLevel.Trace),
                (RelationalEventId.ConnectionClosed, LogLevel.Trace),
                (RelationalEventId.DataReaderDisposing, LogLevel.Trace),
                (RelationalEventId.ConnectionOpening, LogLevel.Trace),
                (RelationalEventId.ConnectionOpened, LogLevel.Trace),
                (RelationalEventId.CommandCreating, LogLevel.Trace),
                (RelationalEventId.CommandCreating, LogLevel.Trace),
                (RelationalEventId.TransactionStarted, LogLevel.Trace),
                (RelationalEventId.TransactionStarting, LogLevel.Trace),
                (RelationalEventId.TransactionCommitted, LogLevel.Trace),
                (RelationalEventId.TransactionCommitting, LogLevel.Trace),
                (RelationalEventId.TransactionDisposed, LogLevel.Trace),
                (RelationalEventId.CommandCreated, LogLevel.Trace)
                ));
    }
}
