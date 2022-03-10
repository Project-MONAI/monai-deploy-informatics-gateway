// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Newtonsoft.Json;

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

            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            builder.HasKey(j => j.InferenceRequestId);

            builder.Property(j => j.TransactionId).IsRequired();
            builder.Property(j => j.Priority).IsRequired();

            builder.Property(j => j.InputMetadata).HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<InferenceRequestMetadata>(v, jsonSerializerSettings));

            builder.Property(j => j.InputResources).HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<List<RequestInputDataResource>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(reqestInputResourceComparer);

            builder.Property(j => j.OutputResources).HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<List<RequestOutputDataResource>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(reqestOutputResourceComparer);

            builder.Property(j => j.State).IsRequired();
            builder.Property(j => j.Status).IsRequired();
            builder.Property(j => j.StoragePath).IsRequired();
            builder.Property(j => j.TryCount).IsRequired();

            builder.Ignore(p => p.Application);
        }
    }
}
