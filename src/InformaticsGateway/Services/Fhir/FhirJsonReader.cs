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
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    internal class FhirJsonReader : IFHirRequestReader
    {
        private readonly InformaticsGatewayConfiguration _config;
        private readonly ILogger<FhirJsonReader> _logger;

        public FhirJsonReader(InformaticsGatewayConfiguration value, ILogger<FhirJsonReader> logger)
        {
            _config = value ?? throw new ArgumentNullException(nameof(value));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FhirStoreResult> GetContent(HttpRequest request, string correlationId, string resourceType, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.Null(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue));

            _logger.ParsingFhirJson();

            var result = new FhirStoreResult
            {
                ResourceType = resourceType,
                RawData = await new StreamReader(request.Body).ReadToEndAsync().ConfigureAwait(false)
            };

            var jsonDoc = JsonNode.Parse(result.RawData);

            if (jsonDoc[Resources.PropertyResourceType] is not null)
            {
                result.InternalResourceType = jsonDoc[Resources.PropertyResourceType].GetValue<string>();
            }

            SetIdIfMIssing(correlationId, jsonDoc);

            result.RawData = jsonDoc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            var fileMetadata = new FhirFileStorageMetadata(correlationId, result.InternalResourceType, correlationId, Api.Rest.FhirStorageFormat.Json);
            fileMetadata.SetDataStream(result.RawData);

            result.Metadata = fileMetadata;
            return result;
        }

        private static void SetIdIfMIssing(string correlationId, JsonNode jsonDoc)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.Null(jsonDoc, nameof(jsonDoc));

            if (string.IsNullOrWhiteSpace(jsonDoc[Resources.PropertyId]?.GetValue<string>()))
            {
                jsonDoc[Resources.PropertyId] = correlationId;
            }
        }
    }
}
