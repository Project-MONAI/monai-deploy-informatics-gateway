// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.EntityFrameworkCore;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class SourceApplicationEntityConfiguration : IEntityTypeConfiguration<SourceApplicationEntity>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SourceApplicationEntity> builder)
        {
            builder.HasKey(j => j.Name);
            builder.Property(j => j.AeTitle).IsRequired();
            builder.Property(j => j.HostIp).IsRequired();
        }
    }
}
