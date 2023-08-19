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

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Database.EntityFramework
{
#pragma warning disable CS8604, CS8603

    internal class RemoteAppExecutionConfiguration : IEntityTypeConfiguration<RemoteAppExecution>
    {
        public void Configure(EntityTypeBuilder<RemoteAppExecution> builder)
        {
            var dictValueComparer = new ValueComparer<Dictionary<string, string>>(
                (c1, c2) => c1!.Equals(c2),
                c => c.GetHashCode(),
                c => c.ToDictionary(entry => entry.Key, entry => entry.Value));

            var jsonSerializerSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            builder.HasKey(j => j.Id);

            builder.Property(j => j.WorkflowInstanceId).IsRequired();
            builder.Property(j => j.ExportTaskId).IsRequired();
            builder.Property(j => j.CorrelationId).IsRequired();
            builder.Property(j => j.RequestTime).IsRequired();

            builder.Property(j => j.StudyInstanceUid).IsRequired();
            builder.Property(j => j.SeriesInstanceUid).IsRequired();
            builder.Property(j => j.SopInstanceUid).IsRequired();

            builder.Property(j => j.OriginalValues)
                .HasConversion(
                        v => JsonSerializer.Serialize(v, jsonSerializerSettings),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(dictValueComparer);

            builder.HasIndex(p => new { p.WorkflowInstanceId, p.ExportTaskId, p.StudyInstanceUid, p.SeriesInstanceUid }, "idx_remoteapp_all");
            builder.HasIndex(p => new { p.WorkflowInstanceId, p.ExportTaskId, p.StudyInstanceUid }, "idx_remoteapp_study");
            builder.HasIndex(p => p.SopInstanceUid, "idx_remoteapp_instance");
        }
    }

#pragma warning restore CS8604, CS8603
}
