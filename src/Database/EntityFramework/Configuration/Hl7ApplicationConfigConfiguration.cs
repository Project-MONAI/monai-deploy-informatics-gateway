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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{
    internal class Hl7ApplicationConfigConfiguration : IEntityTypeConfiguration<Hl7ApplicationConfigEntity>
    {
        public void Configure(EntityTypeBuilder<Hl7ApplicationConfigEntity> builder)
        {
            builder.HasKey(j => j.Id);
            builder.Property(j => j.DataLink.Key).IsRequired();
            builder.Property(j => j.DataLink.Value).IsRequired();
            builder.Property(j => j.DataMapping.Key).IsRequired();
            builder.Property(j => j.DataMapping.Value).IsRequired();
            builder.Property(j => j.SendingId.Key).IsRequired();
            builder.Property(j => j.SendingId.Value).IsRequired();

            builder.Ignore(p => p.Id);
        }
    }
}
