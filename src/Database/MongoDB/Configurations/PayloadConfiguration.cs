/*
 * Copyright 2021-2023 MONAI Consortium
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

using Monai.Deploy.InformaticsGateway.Api.Storage;
using MongoDB.Bson.Serialization;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Configurations
{
    internal static class PayloadConfiguration
    {
        public static void Configure()
        {
            BsonClassMap.RegisterClassMap<Payload>(j =>
            {
                j.AutoMap();
                j.SetIdMember(j.GetMemberMap(c => c.PayloadId));
                j.MapIdProperty(j => j.PayloadId);

                j.SetIgnoreExtraElements(true);

                j.UnmapProperty(p => p.HasTimedOut);
                j.UnmapProperty(p => p.Elapsed);
                j.UnmapProperty(p => p.Count);
            });

            BsonClassMap.RegisterClassMap<DicomFileStorageMetadata>();
            BsonClassMap.RegisterClassMap<FhirFileStorageMetadata>();
            BsonClassMap.RegisterClassMap<Hl7FileStorageMetadata>();
        }
    }
}