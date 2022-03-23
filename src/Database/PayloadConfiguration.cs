// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class PayloadConfiguration : IEntityTypeConfiguration<Payload>
    {
        public void Configure(EntityTypeBuilder<Payload> builder)
        {
            var fileStorageInfoComparer = new ValueComparer<IReadOnlyList<FileStorageInfo>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            builder.HasKey(j => j.Id);

            builder.Property(j => j.Timeout).IsRequired();
            builder.Property(j => j.Key).IsRequired();
            builder.Property(j => j.DateTimeCreated).IsRequired();
            builder.Property(j => j.RetryCount).IsRequired();
            builder.Property(j => j.State).IsRequired();
            builder.Property(j => j.CorrelationId).IsRequired();
            builder.Property(j => j.Files)
                .HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<IReadOnlyList<FileStorageInfo>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(fileStorageInfoComparer);

            builder.Ignore(j => j.CalledAeTitle);
            builder.Ignore(j => j.CallingAeTitle);
            builder.Ignore(j => j.HasTimedOut);
            builder.Ignore(j => j.Count);
        }
    }
}
