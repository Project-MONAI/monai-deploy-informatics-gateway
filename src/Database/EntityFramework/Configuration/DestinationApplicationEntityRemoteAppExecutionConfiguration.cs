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
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{

    internal class DestinationApplicationEntityRemoteAppExecutionConfiguration : IEntityTypeConfiguration<DestinationApplicationEntityRemoteAppExecution>
    {
        public void Configure(EntityTypeBuilder<DestinationApplicationEntityRemoteAppExecution> builder)
        {
            builder.HasKey(dr => new { dr.DestinationApplicationEntityName, dr.RemoteAppExecutionId });
            builder.HasOne(bc => bc.RemoteAppExecution)
                .WithMany(b => b.DestinationApplicationEntityRemoteAppExecutions)
                .HasForeignKey(bc => bc.RemoteAppExecutionId);
            builder.HasOne(bc => bc.DestinationApplicationEntity)
                .WithMany(c => c.DestinationApplicationEntityRemoteAppExecutions)
                .HasForeignKey(bc => bc.DestinationApplicationEntityName);

        }
    }
}