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

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    internal class FhirJsonFormatters : SystemTextJsonOutputFormatter
    {
        public FhirJsonFormatters(JsonSerializerOptions jsonSerializerOptions) : base(jsonSerializerOptions)
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(ContentTypes.ApplicationFhirJson);
        }
    }

    internal class FhirXmlFormatters : XmlDataContractSerializerOutputFormatter

    {
        public FhirXmlFormatters()
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(ContentTypes.ApplicationFhirXml);
        }
    }
}
