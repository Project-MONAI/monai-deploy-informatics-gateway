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

using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Services.Common
{
    public class DicomValue
    {
        [JsonPropertyName("vr")]
        public string Vr { get; set; } = string.Empty;

        [JsonPropertyName("Value")]
        public object[] Value { get; set; } = System.Array.Empty<object>();
    }

    public static class DicomTagConstants
    {
        public const string PatientIdTag = "00100020";

        public const string PatientNameTag = "00100010";

        public const string PatientSexTag = "00100040";

        public const string PatientDateOfBirthTag = "00100030";

        public const string PatientAgeTag = "00101010";

        public const string PatientHospitalIdTag = "00100021";
    }
}
