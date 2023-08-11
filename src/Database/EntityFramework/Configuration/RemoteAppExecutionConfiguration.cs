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

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{
#pragma warning disable CS8604, CS8603

    internal class RemoteAppExecutionConfiguration : IEntityTypeConfiguration<RemoteAppExecution>
    {
        public void Configure(EntityTypeBuilder<RemoteAppExecution> builder)
        {
            var jsonSerializerSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            builder.HasKey(j => j.Id);

            builder.Property(j => j.OutgoingUid).IsRequired();
            builder.Property(j => j.ExportTaskId).IsRequired();
            //builder.Property(j => j.Status).IsRequired();
            builder.Property(j => j.CorrelationId).IsRequired();
            builder.Property(j => j.OriginalValues)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonSerializerSettings));
            builder.Property(j => j.ProxyValues)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonSerializerSettings));

            builder.Property(j => j.Files)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<List<string>>(v, jsonSerializerSettings));

            builder.HasIndex(p => p.OutgoingUid, "idx_outgoing_key");

        }
    }

#pragma warning restore CS8604, CS8603
}
