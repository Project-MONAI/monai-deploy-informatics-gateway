// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class MonaiApplicationEntityConfiguration : IEntityTypeConfiguration<MonaiApplicationEntity>
    {
        public void Configure(EntityTypeBuilder<MonaiApplicationEntity> builder)
        {
            var valueComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            builder.HasKey(j => j.Name);

            builder.Property(j => j.AeTitle).IsRequired();
            builder.Property(j => j.Timeout).IsRequired();
            builder.Property(j => j.Grouping).IsRequired();
            builder.Property(j => j.Workflows)
                .HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<List<string>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(valueComparer);
            builder.Property(j => j.IgnoredSopClasses)
                .HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<List<string>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(valueComparer);
        }
    }
}
