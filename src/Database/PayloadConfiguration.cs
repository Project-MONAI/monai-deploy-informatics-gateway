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
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class PayloadConfiguration : IEntityTypeConfiguration<Payload>
    {
        public void Configure(EntityTypeBuilder<Payload> builder)
        {
            var valueComparer = new ValueComparer<IList<FileStorageInfo>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            builder.HasKey(j => j.Id);

            builder.Property(j => j.Timeout).IsRequired();
            builder.Property(j => j.Key).IsRequired();
            builder.Property(j => j.RetryCount).IsRequired();
            builder.Property(j => j.State).IsRequired();
            builder.Property(j => j.Files)
                .HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<IList<FileStorageInfo>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(valueComparer);
        }
    }
}
