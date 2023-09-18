/*
 * Copyright 2022-2023 MONAI Consortium
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
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.Messaging.Events;

namespace Monai.Deploy.InformaticsGateway.Services.Fhir
{
    internal class FhirXmlReader : IFHirRequestReader
    {
        private readonly ILogger<FhirXmlReader> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IFileSystem _fileSystem;

        public FhirXmlReader(ILogger<FhirXmlReader> logger, IOptions<InformaticsGatewayConfiguration> options, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public async Task<FhirStoreResult> GetContentAsync(HttpRequest request, string correlationId, string resourceType, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.NullOrInvalidInput(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue), (value) =>
            {
                return value.MediaType.Value.Equals(ContentTypes.ApplicationFhirXml, StringComparison.OrdinalIgnoreCase);
            });

            _logger.ParsingFhirXml();

            var result = new FhirStoreResult
            {
                ResourceType = resourceType,
                RawData = await new StreamReader(request.Body).ReadToEndAsync().ConfigureAwait(false)
            };

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result.RawData);
            var rootNode = xmlDocument.DocumentElement;

            if (!rootNode!.NamespaceURI.Equals(Resources.FhirXmlNamespace, StringComparison.OrdinalIgnoreCase))
            {
                throw new FhirStoreException(correlationId, $"XML content is not a valid FHIR resource.", IssueType.Invalid);
            }

            var xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
            xmlNamespaceManager.AddNamespace(Resources.XmlNamespacePrefix, Resources.FhirXmlNamespace);

            var resourceId = SetIdIfMIssing(correlationId, xmlNamespaceManager, rootNode);

            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
            {
                xmlDocument.Save(xmlWriter);
            }

            result.RawData = sb.ToString();
            result.InternalResourceType = rootNode.Name;

            var fileMetadata = new FhirFileStorageMetadata(correlationId, result.InternalResourceType, resourceId, Api.Rest.FhirStorageFormat.Xml, DataService.FHIR, request.HttpContext.Connection.RemoteIpAddress!.ToString());
            await fileMetadata.SetDataStream(result.RawData, _options.Value.Storage.TemporaryDataStorage, _fileSystem, _options.Value.Storage.LocalTemporaryStoragePath);

            result.Metadata = fileMetadata;
            return result;
        }

        private static string SetIdIfMIssing(string correlationId, XmlNamespaceManager xmlNamespaceManager, XmlElement rootNode)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.Null(xmlNamespaceManager, nameof(xmlNamespaceManager));
            Guard.Against.Null(rootNode, nameof(rootNode));

            var idNode = rootNode.SelectSingleNode($"{Resources.XmlNamespacePrefix}:{Resources.PropertyId}", xmlNamespaceManager);
            if (idNode is null)
            {
                idNode = rootNode.OwnerDocument.CreateElement(Resources.PropertyId, Resources.FhirXmlNamespace);
                rootNode.PrependChild(idNode);
            }

            if (string.IsNullOrWhiteSpace(idNode.Attributes![Resources.AttributeValue]?.Value))
            {
                var value = idNode.OwnerDocument!.CreateAttribute(Resources.AttributeValue);
                value.Value = correlationId;
                idNode.Attributes.Append(value);
            }

            return idNode.Attributes[Resources.AttributeValue]!.Value;
        }
    }
}