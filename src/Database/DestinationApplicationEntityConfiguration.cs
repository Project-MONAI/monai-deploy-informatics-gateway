// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class DestinationApplicationEntityConfiguration : IEntityTypeConfiguration<DestinationApplicationEntity>
    {
        public void Configure(EntityTypeBuilder<DestinationApplicationEntity> builder)
        {
            builder.HasKey(j => j.Name);
            builder.Property(j => j.AeTitle).IsRequired();
            builder.Property(j => j.Port).IsRequired();
            builder.Property(j => j.HostIp).IsRequired();
        }
    }
}
