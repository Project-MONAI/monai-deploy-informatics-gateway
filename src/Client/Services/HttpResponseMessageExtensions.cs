/*
 * Copyright 2021-2022 MONAI Consortium
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
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    internal static class HttpResponseMessageExtensions
    {
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
