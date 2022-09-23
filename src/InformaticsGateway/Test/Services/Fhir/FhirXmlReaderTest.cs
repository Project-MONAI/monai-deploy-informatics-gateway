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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Fhir;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Fhir
{
    public class FhirXmlReaderTest
    {
        private readonly Mock<ILogger<FhirXmlReader>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IFileSystem _fileSystem;

        public FhirXmlReaderTest()
        {
            _logger = new Mock<ILogger<FhirXmlReader>>();
            _options = Options.Create<InformaticsGatewayConfiguration>(new InformaticsGatewayConfiguration());
            _fileSystem = new MockFileSystem();
            _options.Value.Storage.TemporaryDataStorage = TemporaryDataStorageLocation.Memory;
        }

        [Fact]
        public async Task GetContentAsync_WhenCalled_EnsuresArgumentsAreValid()
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var reader = new FhirXmlReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(null, null, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(request.Object, null, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(request.Object, correlationId, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(request.Object, correlationId, resourceType, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentException>(async () => await reader.GetContentAsync(request.Object, correlationId, resourceType, new MediaTypeHeaderValue(ContentTypes.ApplicationFhirJson), CancellationToken.None));
        }

        [Fact]
        public async Task GetContentAsync_WhenCalledWithEmptyContent_ThrowsException()
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirXml);
            var reader = new FhirXmlReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);
            });

            await Assert.ThrowsAsync<XmlException>(async () =>
            {
                request.Setup(p => p.Body).Returns(MemoryStream.Null);
                await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);
            });
        }

        [Fact]
        public async Task GetContentAsync_WhenCalledWithNonXmlContent_ThrowsException()
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirXml);
            var reader = new FhirXmlReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAsync<XmlException>(async () =>
            {
                var data = System.Text.Encoding.UTF8.GetBytes("save the world");
                using var stream = new System.IO.MemoryStream();
                await stream.WriteAsync(data, 0, data.Length);
                stream.Position = 0;
                request.Setup(p => p.Body).Returns(stream);
                await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);
            });
        }

        [Fact]
        public async Task GetContentAsync_WhenCalledWithNonFhirContent_ThrowsException()
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirXml);
            var reader = new FhirXmlReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAsync<FhirStoreException>(async () =>
            {
                var data = System.Text.Encoding.UTF8.GetBytes("<data>missing-correct-namespace</data>");
                using var stream = new System.IO.MemoryStream();
                await stream.WriteAsync(data, 0, data.Length);
                stream.Position = 0;
                request.Setup(p => p.Body).Returns(stream);
                await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);
            });
        }

        [Theory]
        [InlineData("<Patient xmlns=\"http://hl7.org/fhir\"><name><family value=\"Monai\"/><given value=\"Deploy\"/></name></Patient>")]
        [InlineData("<Patient xmlns=\"http://hl7.org/fhir\"><id /><name><family value=\"Monai\"/><given value=\"Deploy\"/></name></Patient>")]
        [InlineData("<Patient xmlns=\"http://hl7.org/fhir\"><id value=\" \"/><name><family value=\"Monai\"/><given value=\"Deploy\"/></name></Patient>")]
        public async Task GetContentAsync_WhenCalledWithNoId_ReturnsOriginalWithId(string xml)
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirXml);
            var reader = new FhirXmlReader(_logger.Object, _options, _fileSystem);

            var data = System.Text.Encoding.UTF8.GetBytes(xml);
            using var stream = new System.IO.MemoryStream();
            await stream.WriteAsync(data, 0, data.Length);
            stream.Position = 0;
            request.Setup(p => p.Body).Returns(stream);
            var results = await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);

            Assert.Equal(correlationId, results.Metadata.CorrelationId);
            Assert.Equal(correlationId, results.Metadata.ResourceId);
            Assert.Equal(resourceType, results.InternalResourceType);
            Assert.IsType<MemoryStream>(results.Metadata.File.Data);
            Assert.NotNull(results.Metadata);
            Assert.Contains($"<id value=\"{correlationId}\" />", results.RawData);
        }
    }
}
