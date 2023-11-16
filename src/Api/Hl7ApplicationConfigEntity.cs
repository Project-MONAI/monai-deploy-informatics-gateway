/*
 * Copyright 2022 MONAI Consortium
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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Common;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Api
{
    public class Hl7ApplicationConfigEntity : MongoDBEntityBase
    {
        /// <summary>
        /// Gets or sets the sending identifier.
        /// </summary>
        [JsonProperty("sending_identifier")]
        public KeyValuePair<string, string> SendingId { get; set; }

        /// <summary>
        /// Gets or sets the data link.
        /// Value is either PatientId or StudyInstanceUid
        /// </summary>
        [JsonProperty("data_link")]
        public KeyValuePair<string, DataLinkType> DataLink { get; set; }

        /// <summary>
        /// Gets or sets the data mapping.
        /// Value is a DICOM Tag
        /// </summary>
        [JsonProperty("data_mapping")]
        public Dictionary<string, string> DataMapping { get; set; } = new();

        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(SendingId.Key))
                errors.Add($"{nameof(SendingId.Key)} is missing.");
            if (string.IsNullOrWhiteSpace(SendingId.Value))
                errors.Add($"{nameof(SendingId.Value)} is missing.");
            if (string.IsNullOrWhiteSpace(DataLink.Key))
                errors.Add($"{nameof(DataLink.Key)} is missing.");
            if (DataMapping.IsNullOrEmpty())
                errors.Add($"{nameof(DataMapping)} is missing values.");

            for (var idx = 0; idx < DataMapping.Count; idx++)
            {
                var dataMapKvp = DataMapping.ElementAt(idx);

                if (string.IsNullOrWhiteSpace(dataMapKvp.Key) || dataMapKvp.Value.Length < 8)
                {
                    if (string.IsNullOrWhiteSpace(dataMapKvp.Key))
                        errors.Add($"{nameof(DataMapping)} is missing a name at index {idx}.");

                    if (dataMapKvp.Value.Length < 8)
                        errors.Add($"{nameof(DataMapping)} ({dataMapKvp.Key}) @ index {idx} is not a valid DICOM Tag.");

                    continue;
                }

                try
                {
                    DicomTag.Parse(dataMapKvp.Value);
                }
                catch (Exception e)
                {
                    errors.Add($"DataMapping.Value is not a valid DICOM Tag. {e.Message}");
                }
            }

            return errors;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public enum DataLinkType
    {
        PatientId,
        StudyInstanceUid
    }
}
