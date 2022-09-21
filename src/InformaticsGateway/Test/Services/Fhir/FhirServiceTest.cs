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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.Connectors;
using Monai.Deploy.InformaticsGateway.Services.Fhir;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Fhir
{
    public class FhirServiceTest
    {
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        private readonly Mock<ILogger<FhirService>> _logger;
        private readonly Mock<ILogger<FhirJsonReader>> _loggerJson;
        private readonly Mock<ILogger<FhirXmlReader>> _loggerXml;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IPayloadAssembler> _payloadAssembler;
        private readonly Mock<IObjectUploadQueue> _uploadQueue;

        private readonly Mock<IServiceScope> _serviceScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<HttpRequest> _httpRequest;

        public FhirServiceTest()
        {
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _options = Options.Create(new InformaticsGatewayConfiguration());

            _serviceScope = new Mock<IServiceScope>();

            _payloadAssembler = new Mock<IPayloadAssembler>();
            _uploadQueue = new Mock<IObjectUploadQueue>();
            _logger = new Mock<ILogger<FhirService>>();
            _loggerJson = new Mock<ILogger<FhirJsonReader>>();
            _loggerXml = new Mock<ILogger<FhirXmlReader>>();
            _fileSystem = new Mock<IFileSystem>();

            _httpRequest = new Mock<HttpRequest>();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);

            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => _loggerJson.Object);
            services.AddScoped(p => _loggerXml.Object);
            services.AddScoped(p => _uploadQueue.Object);
            services.AddScoped(p => _payloadAssembler.Object);
            services.AddScoped(p => _fileSystem.Object);
            _serviceProvider = services.BuildServiceProvider();
            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [RetryFact]
        public void GivenAFhirService_WhenInitialized_ExpectParametersToBeValidated()
        {
            Assert.Throws<ArgumentNullException>(() => new FhirService(null, null));
            Assert.Throws<ArgumentNullException>(() => new FhirService(_serviceScopeFactory.Object, null));

            new FhirService(_serviceScopeFactory.Object, _options);
        }

        [RetryFact]
        public async Task StoreAsync_WhenCalled_ShallValidateParametersAsync()
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var service = new FhirService(_serviceScopeFactory.Object, _options);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.StoreAsync(null, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.StoreAsync(_httpRequest.Object, null, null, CancellationToken.None));
            await Assert.ThrowsAsync<UnsupportedContentTypeException>(async () => await service.StoreAsync(_httpRequest.Object, correlationId, null, CancellationToken.None));
            await Assert.ThrowsAsync<UnsupportedContentTypeException>(async () => await service.StoreAsync(_httpRequest.Object, correlationId, resourceType, CancellationToken.None));
        }

        [RetryTheory]
        [InlineData(ContentTypes.ApplicationFhirXml, "<Patient xmlns=\"http://hl7.org/fhir\"><name><family value=\"Monai\"/><given value=\"Deploy\"/></name></Patient>")]
        [InlineData(ContentTypes.ApplicationFhirJson, "{\"resourceType\":\"Patient\",\"name\":[{\"use\":\"official\",\"family\":\"Monai\",\"given\":[\"Deploy\"]}]}")]
        public async Task StoreAsync_WhenCalledWithInvalidContent_ShallThrowFhirStoreException(string contentType, string content)
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "WHAT";
            var service = new FhirService(_serviceScopeFactory.Object, _options);

            _httpRequest.Setup(p => p.ContentType).Returns(contentType);
            _httpRequest.Setup(p => p.Body).Returns(MemoryStream.Null);
            await Assert.ThrowsAsync<FhirStoreException>(async () => await service.StoreAsync(_httpRequest.Object, correlationId, resourceType, CancellationToken.None));
        }

        [RetryTheory]
        [InlineData(ContentTypes.ApplicationFhirXml, "<Patient xmlns=\"http://hl7.org/fhir\"><name><family value=\"Monai\"/><given value=\"Deploy\"/></name></Patient>")]
        [InlineData(ContentTypes.ApplicationFhirJson, "{\"resourceType\":\"Patient\",\"name\":[{\"use\":\"official\",\"family\":\"Monai\",\"given\":[\"Deploy\"]}]}")]
        public async Task StoreAsync_WhenCalledWithMismatchingResourceTypes_ShallThrowFhirStoreException(string contentType, string content)
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "WHAT";
            var service = new FhirService(_serviceScopeFactory.Object, _options);

            _httpRequest.Setup(p => p.ContentType).Returns(contentType);
            _httpRequest.Setup(p => p.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
            await Assert.ThrowsAsync<FhirStoreException>(async () => await service.StoreAsync(_httpRequest.Object, correlationId, resourceType, CancellationToken.None));
        }

        [RetryTheory]
        [InlineData(ContentTypes.ApplicationFhirXml, "<Patient xmlns=\"http://hl7.org/fhir\"><name><family value=\"Monai\"/><given value=\"Deploy\"/></name></Patient>")]
        [InlineData(ContentTypes.ApplicationFhirJson, "{\"resourceType\":\"Patient\",\"name\":[{\"use\":\"official\",\"family\":\"Monai\",\"given\":[\"Deploy\"]}]}")]
        public async Task StoreAsync_WhenCalledWithValidContent_ShallQueueForProessing(string contentType, string content)
        {
            var correlationId = Guid.NewGuid().ToString();
            var resourceType = "Patient";
            var service = new FhirService(_serviceScopeFactory.Object, _options);

            _httpRequest.Setup(p => p.ContentType).Returns(contentType);
            _httpRequest.Setup(p => p.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
            var results = await service.StoreAsync(_httpRequest.Object, correlationId, resourceType, CancellationToken.None);

            Assert.Equal(StatusCodes.Status201Created, results.StatusCode);

            _uploadQueue.Verify(p => p.Queue(It.IsAny<FileStorageMetadata>()), Times.Once());
            _payloadAssembler.Verify(p => p.Queue(It.IsAny<string>(), It.IsAny<FileStorageMetadata>(), It.IsAny<uint>()), Times.Once());
        }
    }
}
