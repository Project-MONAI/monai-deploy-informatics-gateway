// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Client.Common;
using Newtonsoft.Json;

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
                var json = await responseMessage.Content.ReadAsStringAsync();
                var problem = JsonConvert.DeserializeObject<ProblemDetails>(json);

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

            var content = await responseMessage.Content.ReadAsStringAsync();
            throw new HttpRequestException(content);
        }
    }
}
