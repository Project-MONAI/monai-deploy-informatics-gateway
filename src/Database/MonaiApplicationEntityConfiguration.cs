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

/*
 * Apache License, Version 2.0
 * Copyright 2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Monai.Deploy.InformaticsGateway.Database
{
    internal class MonaiApplicationEntityConfiguration : IEntityTypeConfiguration<MonaiApplicationEntity>
    {
        public void Configure(EntityTypeBuilder<MonaiApplicationEntity> builder)
        {
            var valueComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            builder.HasKey(j => j.Name);

            builder.Property(j => j.AeTitle).IsRequired();
            builder.Property(j => j.Timeout).IsRequired();
            builder.Property(j => j.Grouping).IsRequired();
            builder.Property(j => j.Workflows)
                .HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<List<string>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(valueComparer);
            builder.Property(j => j.IgnoredSopClasses)
                .HasConversion(
                        v => JsonConvert.SerializeObject(v, jsonSerializerSettings),
                        v => JsonConvert.DeserializeObject<List<string>>(v, jsonSerializerSettings))
                .Metadata.SetValueComparer(valueComparer);
        }
    }
}
