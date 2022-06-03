// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    internal static class HttpResponseMessageExtensions
    {
        public static readonly JsonSerializerOptions JsonSerializationOptions = new(JsonSerializerDefaults.Web);

        static HttpResponseMessageExtensions()
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

        public static async Task EnsureSuccessStatusCodeWithProblemDetails(this HttpResponseMessage responseMessage, ILogger logger = null)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                return;
            }

            try
            {
                var json = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                var problem = JsonSerializer.Deserialize<ProblemDetails>(json, Configuration.JsonSerializationOptions);

                if (problem?.Status != 0)
                {
                    throw new ProblemException(problem);
                }
            }
            catch (Exception ex)
            {
                if (ex is ProblemException)
                {
                    throw;
                }
                logger?.Log(LogLevel.Trace, ex, "Error reading server side problem.");
            }

            var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException(content);
        }
    }
}
