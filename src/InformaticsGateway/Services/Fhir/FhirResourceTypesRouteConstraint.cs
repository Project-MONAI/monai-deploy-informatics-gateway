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

using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    internal class FhirResourceTypesRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            Guard.Against.Null(httpContext, nameof(httpContext));
            Guard.Against.Null(route, nameof(route));
            Guard.Against.NullOrWhiteSpace(routeKey, nameof(routeKey));
            Guard.Against.Null(values, nameof(values));

            return (values.TryGetValue(Resources.RouteNameResourceType, out var resourceTypeObject) &&
                resourceTypeObject is string resourceType &&
                !string.IsNullOrWhiteSpace(resourceType));
        }
    }
}
