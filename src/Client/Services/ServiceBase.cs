// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Client.Services
{
    internal abstract class ServiceBase
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;
        protected string RequestServicePrefix { get; private set; } = string.Empty;
        protected JsonSerializerOptions JsonSerializationOptions 
        {
            get
            {
                return HttpResponseMessageExtensions.JsonSerializationOptions;
            }
        
        }


        protected ServiceBase(HttpClient httpClient, ILogger logger = null)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Logger = logger;
        }
    }
}
