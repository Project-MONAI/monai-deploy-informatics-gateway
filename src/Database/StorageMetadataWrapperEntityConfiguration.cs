/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2021 NVIDIA Corporation
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

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class StorageMetadataWrapperEntityConfiguration : IEntityTypeConfiguration<StorageMetadataWrapper>
    {
        public void Configure(EntityTypeBuilder<StorageMetadataWrapper> builder)
        {
            builder.HasKey(j => new
            {
                j.CorrelationId,
                j.Identity
            });
            builder.Property(j => j.CorrelationId);
            builder.Property(j => j.Value);
            builder.Property(j => j.TypeName);

            builder.HasIndex(p => new { p.CorrelationId, p.Identity }, "idx_storagemetadata_ids").IsUnique();
            builder.HasIndex(p => p.CorrelationId, "idx_storagemetadata_correlation").IsUnique();
            builder.HasIndex(p => p.IsUploaded, "idx_storagemetadata_uploaded");
        }
    }
}
