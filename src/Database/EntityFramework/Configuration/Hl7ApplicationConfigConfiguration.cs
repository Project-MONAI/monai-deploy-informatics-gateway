/*
 * Copyright 2023 MONAI Consortium
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

using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{
    internal class Hl7ApplicationConfigConfiguration : IEntityTypeConfiguration<Hl7ApplicationConfigEntity>
    {
        public void Configure(EntityTypeBuilder<Hl7ApplicationConfigEntity> builder)
        {
            var valueComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            builder.HasKey(j => j.Name);
            builder.Property(j => j.SendingId).HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<StringKeyValuePair>(v, jsonSerializerSettings)!)
                .IsRequired()
                .Metadata
                .SetValueComparer(
                    new ValueComparer<StringKeyValuePair>(
                        (c1, c2) => c1 == c2,
                        c => c.GetHashCode(),
                        c => c));

            builder.Property(j => j.DataLink).HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<DataKeyValuePair>(v, jsonSerializerSettings)!)
                .IsRequired()
                .Metadata
                .SetValueComparer(
                    new ValueComparer<DataKeyValuePair>(
                        (c1, c2) => c1 == c2,
                        c => c.GetHashCode(),
                        c => c));

            builder.Property(j => j.DateTimeCreated).IsRequired();
            builder.Property(j => j.PlugInAssemblies)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonSerializerSettings)!)
                .Metadata.SetValueComparer(valueComparer);

            builder.Property(j => j.DataMapping)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<List<StringKeyValuePair>>(v, jsonSerializerSettings)!)
                .Metadata
                .SetValueComparer(
                    new ValueComparer<List<StringKeyValuePair>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

            builder.HasIndex(p => p.Name, "idx_hl7_name").IsUnique();

            builder.Ignore(p => p.Id);
        }
    }
}
