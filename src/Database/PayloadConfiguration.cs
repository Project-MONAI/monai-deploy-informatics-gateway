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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class PayloadConfiguration : IEntityTypeConfiguration<Payload>
    {
        public void Configure(EntityTypeBuilder<Payload> builder)
        {
            var metadataComparer = new ValueComparer<List<FileStorageMetadata>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            builder.HasKey(j => j.Id);

            builder.Property(j => j.Timeout).IsRequired();
            builder.Property(j => j.Key).IsRequired();
            builder.Property(j => j.DateTimeCreated).IsRequired();
            builder.Property(j => j.RetryCount).IsRequired();
            builder.Property(j => j.State).IsRequired();
            builder.Property(j => j.CorrelationId).IsRequired();
            builder.Property(j => j.Files)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<List<FileStorageMetadata>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(metadataComparer);

            builder.Ignore(j => j.CalledAeTitle);
            builder.Ignore(j => j.CallingAeTitle);
            builder.Ignore(j => j.HasTimedOut);
            builder.Ignore(j => j.Elapsed);
            builder.Ignore(j => j.Count);

            builder.HasIndex(p => p.State, "idx_payload_state");
            builder.HasIndex(p => new { p.CorrelationId, p.Id }, "idx_payload_ids").IsUnique();
        }
    }
}
