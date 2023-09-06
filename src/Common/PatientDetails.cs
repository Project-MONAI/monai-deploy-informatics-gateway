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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public class PatientDetails
    {
        [JsonPropertyName("patient_id")]
        public string? PatientId { get; set; }

        [JsonPropertyName("patient_name")]
        public string? PatientName { get; set; }

        [JsonPropertyName("patient_sex")]
        public string? PatientSex { get; set; }

        [JsonPropertyName("patient_dob")]
        public DateTime? PatientDob { get; set; }

        [JsonPropertyName("patient_age")]
        public string? PatientAge { get; set; }

        [JsonPropertyName("patient_hospital_id")]
        public string? PatientHospitalId { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
