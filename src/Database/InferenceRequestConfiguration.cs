// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api.Rest;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class InferenceRequestConfiguration : IEntityTypeConfiguration<InferenceRequest>
    {
        public void Configure(EntityTypeBuilder<InferenceRequest> builder)
        {
            var reqestInputResourceComparer = new ValueComparer<IList<RequestInputDataResource>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var reqestOutputResourceComparer = new ValueComparer<IList<RequestOutputDataResource>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            builder.HasKey(j => j.InferenceRequestId);

            builder.Property(j => j.TransactionId).IsRequired();
            builder.Property(j => j.Priority).IsRequired();

            builder.Property(j => j.InputMetadata).HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<InferenceRequestMetadata>(v, jsonSerializerSettings));

            builder.Property(j => j.InputResources).HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<List<RequestInputDataResource>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(reqestInputResourceComparer);

            builder.Property(j => j.OutputResources).HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<List<RequestOutputDataResource>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(reqestOutputResourceComparer);

            builder.Property(j => j.State).IsRequired();
            builder.Property(j => j.Status).IsRequired();
            builder.Property(j => j.StoragePath).IsRequired();
            builder.Property(j => j.TryCount).IsRequired();

            builder.Ignore(p => p.Application);
        }
    }
}
