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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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
    internal class FhirXmlReader : IFHirRequestReader
    {
        private readonly InformaticsGatewayConfiguration _config;
        private readonly ILogger<FhirXmlReader> _logger;

        public FhirXmlReader(InformaticsGatewayConfiguration value, ILogger<FhirXmlReader> logger)
        {
            _config = value ?? throw new ArgumentNullException(nameof(value));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FhirStoreResult> GetContent(HttpRequest request, string correlationId, string resourceType, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.Null(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue));

            _logger.ParsingFhirXml();

            var result = new FhirStoreResult
            {
                ResourceType = resourceType,
                RawData = await new StreamReader(request.Body).ReadToEndAsync().ConfigureAwait(false)
            };

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(result.RawData);

            var xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
            xmlNamespaceManager.AddNamespace(Resources.XmlNamespacePrefix, Resources.XmlNamespace);

            var rootNode = xmlDocument.DocumentElement;
            SetIdIfMIssing(correlationId, xmlNamespaceManager, rootNode);

            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
            {
                xmlDocument.Save(xmlWriter);
            }

            result.RawData = sb.ToString();
            result.InternalResourceType = rootNode.Name;

            var fileMetadata = new FhirFileStorageMetadata(correlationId, result.InternalResourceType, correlationId, Api.Rest.FhirStorageFormat.Xml);
            fileMetadata.SetDataStream(result.RawData);

            result.Metadata = fileMetadata;
            return result;
        }

        private static void SetIdIfMIssing(string correlationId, XmlNamespaceManager xmlNamespaceManager, XmlElement rootNode)
        {
            Guard.Against.NullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.Against.Null(xmlNamespaceManager, nameof(xmlNamespaceManager));
            Guard.Against.Null(rootNode, nameof(rootNode));

            var idNode = rootNode.SelectSingleNode($"{Resources.XmlNamespacePrefix}:{Resources.PropertyId}", xmlNamespaceManager);
            if (idNode is null)
            {
                idNode = rootNode.OwnerDocument.CreateElement(Resources.PropertyId, Resources.XmlNamespace);
                rootNode.PrependChild(idNode);
            }

            if (string.IsNullOrWhiteSpace(idNode.Attributes[Resources.AttributeValue]?.Value))
            {
                var value = idNode.OwnerDocument.CreateAttribute(Resources.AttributeValue);
                value.Value = correlationId;
                idNode.Attributes.Append(value);
            }
        }
    }
}
