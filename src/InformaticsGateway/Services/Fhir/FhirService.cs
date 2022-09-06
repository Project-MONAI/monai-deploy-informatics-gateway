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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Storage;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    internal class FhirService : IFhirService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly ILogger<FhirService> _logger;
        private readonly IPayloadAssembler _payloadAssembler;
        private readonly IObjectUploadQueue _uploadQueue;

        public FhirService(IServiceScopeFactory serviceScopeFactory, IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var scope = _serviceScopeFactory.CreateScope();
            _logger = scope.ServiceProvider.GetService<ILogger<FhirService>>() ?? throw new ServiceNotFoundException(nameof(ILogger<FhirService>));
            _payloadAssembler = scope.ServiceProvider.GetService<IPayloadAssembler>() ?? throw new ServiceNotFoundException(nameof(IPayloadAssembler));
            _uploadQueue = scope.ServiceProvider.GetService<IObjectUploadQueue>() ?? throw new ServiceNotFoundException(nameof(IObjectUploadQueue));
        }

        public async Task<FhirStoreResult> StoreAsync(HttpRequest request, string correlationId, string resourceType, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));

            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeaderValue))
            {
                throw new UnsupportedContentTypeException($"The content type of '{request.ContentType}' is not supported.");
            }

            var reader = GetRequestReader(mediaTypeHeaderValue);
            FhirStoreResult content;
            try
            {
                content = await reader.GetContentAsync(request, correlationId, resourceType, mediaTypeHeaderValue, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new FhirStoreException(correlationId, "The FHIR resource is not a valid JSON object.", IssueType.Structure, ex);
            }
            catch (XmlException ex)
            {
                throw new FhirStoreException(correlationId, "The FHIR resource is not a valid XML object.", IssueType.Structure, ex);
            }

            if (!string.IsNullOrWhiteSpace(resourceType) &&
                !resourceType.Equals(content.InternalResourceType, StringComparison.OrdinalIgnoreCase))
            {
                throw new FhirStoreException(correlationId, $"Provided resource is of type '{content.InternalResourceType}' but request targeted type '{resourceType}'.", IssueType.Invalid);
            }

            _uploadQueue.Queue(content.Metadata);
            await _payloadAssembler.Queue(correlationId, content.Metadata, Resources.PayloadAssemblerTimeout).ConfigureAwait(false);
            _logger.QueuedStowInstance();

            content.StatusCode = StatusCodes.Status201Created;
            return content;
        }

        private IFHirRequestReader GetRequestReader(MediaTypeHeaderValue mediaTypeHeaderValue)
        {
            Guard.Against.Null(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue));

            var scope = _serviceScopeFactory.CreateScope();
            if (mediaTypeHeaderValue.MediaType.Equals(ContentTypes.ApplicationFhirJson, StringComparison.OrdinalIgnoreCase))
            {
                var logger = scope.ServiceProvider.GetService<ILogger<FhirJsonReader>>() ?? throw new ServiceNotFoundException(nameof(ILogger<FhirJsonReader>));
                return new FhirJsonReader(logger);
            }

            if (mediaTypeHeaderValue.MediaType.Equals(ContentTypes.ApplicationFhirXml, StringComparison.OrdinalIgnoreCase))
            {
                var logger = scope.ServiceProvider.GetService<ILogger<FhirXmlReader>>() ?? throw new ServiceNotFoundException(nameof(ILogger<FhirXmlReader>));
                return new FhirXmlReader(logger);
            }

            throw new UnsupportedContentTypeException($"Media type of '{mediaTypeHeaderValue.MediaType}' is not supported.");
        }
    }
}
