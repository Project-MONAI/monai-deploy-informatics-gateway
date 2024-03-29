﻿/*
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
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api.Models;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{
    internal class ExternalAppDetailsConfiguration : IEntityTypeConfiguration<ExternalAppDetails>
    {
        public void Configure(EntityTypeBuilder<ExternalAppDetails> builder)
        {
            var jsonSerializerSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            builder.HasKey(j => j.Id);

            builder.Property(j => j.StudyInstanceUid).IsRequired();
            builder.Property(j => j.WorkflowInstanceId).IsRequired();
            builder.Property(j => j.DateTimeCreated).IsRequired();
            builder.Property(j => j.CorrelationId).IsRequired();
            builder.Property(j => j.ExportTaskID).IsRequired();
        }
    }
}
