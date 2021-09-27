// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class FileStorageInfoConfiguration : IEntityTypeConfiguration<FileStorageInfo>
    {
        public void Configure(EntityTypeBuilder<FileStorageInfo> builder)
        {
            var valueComparer = new ValueComparer<string[]>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToArray());
            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            builder.HasKey(j => j.Id);
            builder.Property(j => j.FilePath).IsRequired();
            builder.Property(j => j.CorrelationId).IsRequired();
            builder.Property(j => j.StorageRootPath).IsRequired();
            builder.Property(j => j.Received).IsRequired();
            builder.Property(j => j.Timestamp).IsRowVersion();

            builder.Property(j => j.Applications).HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<string[]>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(valueComparer);
        }
    }
}
