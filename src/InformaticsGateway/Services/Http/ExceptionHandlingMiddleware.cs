// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    /// <summary>
    /// The StateEnumConstraint is used in routing contraint to help convert enum string used in routes to State enum.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ExceptionHandlingMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var dictionary = new System.Collections.Generic.Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "CorrelationId", Guid.NewGuid().ToString("D") },
                };
                using (_logger.BeginScope(dictionary))
                {
                    await _next(context).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, $"HTTP error in request {context.Request.Path}.");
                await HandleExceptionAsync(context, ex).ConfigureAwait(false);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var code = HttpStatusCode.InternalServerError; // 500 if unexpected

            if (ex is ArgumentException) code = HttpStatusCode.BadRequest;

            var result = JsonConvert.SerializeObject(new ProblemDetails { Title = ex.Message, Detail = ex.ToString() });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            return context.Response.WriteAsync(result);
        }
    }
}
