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

using Monai.Deploy.InformaticsGateway.Api.Rest;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Api.Test.Rest
{
    public class InferenceRequestDetailsTest
    {
        [Theory]
        [InlineData(FhirStorageFormat.Xml, FhirVersion.R1, "application/fhir+xml; fhirVersion=0.0")]
        [InlineData(FhirStorageFormat.Json, FhirVersion.R2, "application/fhir+json; fhirVersion=1.0")]
        [InlineData(FhirStorageFormat.Xml, FhirVersion.R3, "application/fhir+xml; fhirVersion=3.0")]
        [InlineData(FhirStorageFormat.Json, FhirVersion.R4, "application/fhir+json; fhirVersion=4.0")]
        public void GivenFhirStorageFormatAndType_WhenBuildFhirAcceptHeaderIsCalled_ExpectAValidHeader(FhirStorageFormat format, FhirVersion version, string expectedHeader)
        {
            var details = new InferenceRequestDetails
            {
                FhirFormat = format,
                FhirVersion = version
            };

            Assert.Equal(expectedHeader, details.FhirAcceptHeader);
        }
    }
}
