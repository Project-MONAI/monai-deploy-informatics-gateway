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

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Monai.Deploy.InformaticsGateway.Services.Fhir;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Fhir
{
    public class FhirResourceTypesRouteConstraintTest
    {
        [Fact]
        public void Match_WhenCalled_ShallValidateParameters()
        {
            var httpContext = new Mock<HttpContext>();
            var router = new Mock<IRouter>();
            var routeKey = "key";
            var routeValueDictionary = new RouteValueDictionary();

            var routeConstraint = new FhirResourceTypesRouteConstraint();

            Assert.Throws<ArgumentNullException>(() => routeConstraint.Match(null, null, null, null, RouteDirection.IncomingRequest));
            Assert.Throws<ArgumentNullException>(() => routeConstraint.Match(httpContext.Object, null, null, null, RouteDirection.IncomingRequest));
            Assert.Throws<ArgumentNullException>(() => routeConstraint.Match(httpContext.Object, router.Object, null, null, RouteDirection.IncomingRequest));
            Assert.Throws<ArgumentNullException>(() => routeConstraint.Match(httpContext.Object, router.Object, routeKey, null, RouteDirection.IncomingRequest));
        }

        [Fact]
        public void Match_WhenCalledWithInvalidResourceType_ReturnsFalse()
        {
            var httpContext = new Mock<HttpContext>();
            var router = new Mock<IRouter>();
            var routeKey = "key";
            var routeValueDictionary = new RouteValueDictionary();

            var routeConstraint = new FhirResourceTypesRouteConstraint();

            Assert.False(routeConstraint.Match(httpContext.Object, router.Object, routeKey, routeValueDictionary, RouteDirection.IncomingRequest));
        }

        [Fact]
        public void Match_WhenCalledWithValidResourceType_ReturnsTrue()
        {
            var httpContext = new Mock<HttpContext>();
            var router = new Mock<IRouter>();
            var routeKey = "key";
            var routeValueDictionary = new RouteValueDictionary();
            routeValueDictionary.Add(Resources.RouteNameResourceType, "Tao");

            var routeConstraint = new FhirResourceTypesRouteConstraint();

            Assert.True(routeConstraint.Match(httpContext.Object, router.Object, routeKey, routeValueDictionary, RouteDirection.IncomingRequest));
        }
    }
}
