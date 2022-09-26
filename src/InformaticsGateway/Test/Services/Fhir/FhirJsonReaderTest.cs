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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    public class FhirJsonReaderTest
    {
        private readonly Mock<ILogger<FhirJsonReader>> _logger;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;
        private readonly IFileSystem _fileSystem;

        public FhirJsonReaderTest()
        {
            _logger = new Mock<ILogger<FhirJsonReader>>();
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
            var reader = new FhirJsonReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(null, null, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(request.Object, null, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(request.Object, correlationId, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await reader.GetContentAsync(request.Object, correlationId, resourceType, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentException>(async () => await reader.GetContentAsync(request.Object, correlationId, resourceType, new MediaTypeHeaderValue(ContentTypes.ApplicationFhirXml), CancellationToken.None));
        }

        [Fact]
        public async Task GetContentAsync_WhenCalledWithEmptyContent_ThrowsException()
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirJson);
            var reader = new FhirJsonReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);
            });

            await Assert.ThrowsAnyAsync<JsonException>(async () =>
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
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirJson);
            var reader = new FhirJsonReader(_logger.Object, _options, _fileSystem);

            await Assert.ThrowsAnyAsync<JsonException>(async () =>
            {
                var data = System.Text.Encoding.UTF8.GetBytes("save the world");
                using var stream = new System.IO.MemoryStream();
                await stream.WriteAsync(data, 0, data.Length);
                stream.Position = 0;
                request.Setup(p => p.Body).Returns(stream);
                await reader.GetContentAsync(request.Object, correlationId, resourceType, contentType, CancellationToken.None);
            });
        }

        [Theory]
        [InlineData("{\"resourceType\":\"Patient\",\"name\":[{\"use\":\"official\",\"family\":\"Monai\",\"given\":[\"Deploy\"]}]}")]
        [InlineData("{\"resourceType\":\"Patient\",\"id\":\"\",\"name\":[{\"use\":\"official\",\"family\":\"Monai\",\"given\":[\"Deploy\"]}]}")]
        [InlineData("{\"resourceType\":\"Patient\",\"id\":\" \",\"name\":[{\"use\":\"official\",\"family\":\"Monai\",\"given\":[\"Deploy\"]}]}")]
        public async Task GetContentAsync_WhenCalledWithNoId_ReturnsOriginalWithId(string xml)
        {
            var request = new Mock<HttpRequest>();
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationFhirJson);
            var reader = new FhirJsonReader(_logger.Object, _options, _fileSystem);

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
            Assert.Contains($"\"id\": \"{correlationId}\"", results.RawData);
        }
    }
}
