// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
