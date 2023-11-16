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
using FellowOakDicom;
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
        public KeyValuePair<string, string> DataLink { get; set; }

        /// <summary>
        /// Gets or sets the data mapping.
        /// Value is a DICOM Tag
        /// </summary>
        [JsonProperty("data_mapping")]
        public KeyValuePair<string, string> DataMapping { get; set; }

        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(SendingId.Key))
                errors.Add($"{nameof(SendingId.Key)} is missing.");
            if (string.IsNullOrWhiteSpace(SendingId.Value))
                errors.Add($"{nameof(SendingId.Value)} is missing.");
            if (string.IsNullOrWhiteSpace(DataLink.Key))
                errors.Add($"{nameof(DataLink.Key)} is missing.");
            if (string.IsNullOrWhiteSpace(DataLink.Value))
                errors.Add($"{nameof(DataLink.Value)} is missing.");
            if (string.IsNullOrWhiteSpace(DataMapping.Key))
                errors.Add($"{nameof(DataMapping.Key)} is missing.");
            if (string.IsNullOrWhiteSpace(DataMapping.Value))
                errors.Add($"{nameof(DataMapping.Value)} is missing.");

            if (DataMapping.Value.Length < 8)
            {
                errors.Add($"{nameof(DataMapping.Value)} is not a valid DICOM Tag.");
                return errors;
            }

            try
            {
                DicomTag.Parse(DataMapping.Value);
            }
            catch (Exception e)
            {
                errors.Add($"DataMapping.Value is not a valid DICOM Tag. {e.Message}");
            }

            return errors;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
