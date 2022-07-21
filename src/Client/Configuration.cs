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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monai.Deploy.InformaticsGateway.Client
{
    public static class Configuration
    {
        public static readonly JsonSerializerOptions JsonSerializationOptions = new(JsonSerializerDefaults.Web);

        static Configuration()
        {
            JsonSerializationOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            JsonSerializationOptions.PropertyNameCaseInsensitive = true;
            JsonSerializationOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            JsonSerializationOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            JsonSerializationOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            JsonSerializationOptions.WriteIndented = false;
            JsonSerializationOptions.Converters.Add(new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase, false));
            JsonSerializationOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
        }
    }
}
