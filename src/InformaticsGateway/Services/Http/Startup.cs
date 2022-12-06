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

using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using FellowOakDicom.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework;
using Monai.Deploy.InformaticsGateway.Services.Fhir;
using Monai.Deploy.Security.Authentication.Extensions;

namespace Monai.Deploy.InformaticsGateway.Services.Http
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

#pragma warning disable CA1822 // Mark members as static

        public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1822 // Mark members as static
        {
            services.AddHttpContextAccessor();
            services.AddControllers(opts =>
            {
                opts.RespectBrowserAcceptHeader = true;
                var jsonSerializerOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNameCaseInsensitive = true,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                    WriteIndented = false
                };

                jsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase, false));
                jsonSerializerOptions.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: false, autoValidate: false));
                jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
                opts.OutputFormatters.Add(new FhirJsonFormatters(jsonSerializerOptions));
                opts.OutputFormatters.Add(new FhirXmlFormatters());
            })
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                opts.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                opts.JsonSerializerOptions.WriteIndented = false;
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase, false));
                opts.JsonSerializerOptions.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: false, autoValidate: false));
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            })
            .AddXmlSerializerFormatters();

            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("fhirResource", typeof(FhirResourceTypesRouteConstraint));
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MONAI Deploy Informatics Gateway", Version = "v1" });
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState);

                    var result = new BadRequestObjectResult(problemDetails);

                    result.ContentTypes.Add("application/problem+json");
                    result.ContentTypes.Add("application/problem+xml");

                    return result;
                };
            });

            services.AddMonaiAuthentication();

            services.AddHealthChecks()
                .AddCheck<MonaiHealthCheck>("Informatics Gateway Services")
                .AddDbContextCheck<InformaticsGatewayContext>("Database");
        }

#pragma warning disable CA1822 // Mark members as static

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#pragma warning restore CA1822 // Mark members as static
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MONAI Deploy Informatics Gateway v1"));
            }

            app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(c => new
                        {
                            check = c.Key,
                            result = c.Value.Status.ToString()
                        }),
                    });

                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(result).ConfigureAwait(false);
                }
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpointAuthorizationMiddleware();
            app.UseHttpLogging();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health").AllowAnonymous();
                endpoints.MapControllers().RequireAuthorization();
            });
        }
    }
}
