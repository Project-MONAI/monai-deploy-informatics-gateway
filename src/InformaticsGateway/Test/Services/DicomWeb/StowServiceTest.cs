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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api.Repositories;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.DicomWeb
{
    public class StowServiceTest
    {
        private const string Boundary = "BOUNDARY";

        private readonly Mock<IServiceScopeFactory> _serviceFactory;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly Mock<ILogger<StowService>> _logger;
        private readonly Mock<ILogger<MultipartDicomInstanceReader>> _loggerMultipartDicomInstanceReader;
        private readonly Mock<ILogger<SingleDicomInstanceReader>> _loggerSingleDicomInstanceReader;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<IVirtualApplicationEntityRepository> _repository;
        private readonly Mock<IStreamsWriter> _streamsWriter;
        private readonly IFileSystem _fileSystem;
        private readonly IServiceProvider _serviceProvider;

        public StowServiceTest()
        {
            _serviceFactory = new Mock<IServiceScopeFactory>();
            _configuration = Options.Create(new InformaticsGatewayConfiguration());
            _logger = new Mock<ILogger<StowService>>();
            _loggerMultipartDicomInstanceReader = new Mock<ILogger<MultipartDicomInstanceReader>>();
            _loggerSingleDicomInstanceReader = new Mock<ILogger<SingleDicomInstanceReader>>();
            _serviceScope = new Mock<IServiceScope>();
            _repository = new Mock<IVirtualApplicationEntityRepository>();
            _streamsWriter = new Mock<IStreamsWriter>();
            _fileSystem = new MockFileSystem();

            var services = new ServiceCollection();
            services.AddScoped(p => _logger.Object);
            services.AddScoped(p => _loggerMultipartDicomInstanceReader.Object);
            services.AddScoped(p => _loggerSingleDicomInstanceReader.Object);
            services.AddScoped(p => _streamsWriter.Object);
            services.AddScoped(p => _fileSystem);
            services.AddScoped(p => _repository.Object);

            _serviceProvider = services.BuildServiceProvider();

            _serviceFactory.Setup(p => p.CreateScope())
                .Returns(_serviceScope.Object);
            _serviceScope.SetupGet(p => p.ServiceProvider).Returns(_serviceProvider);
        }

        [Fact(DisplayName = "Constructor Test")]
        public void ConstructorTest()
        {
            Assert.Throws<ArgumentNullException>(() => new StowService(null, null));
            Assert.Throws<ArgumentNullException>(() => new StowService(_serviceFactory.Object, null));
            var exception = Record.Exception(() => new StowService(_serviceFactory.Object, _configuration));

            Assert.Null(exception);
        }

        [Fact(DisplayName = "StoreAsync - Throws when virtual AE cannot be found")]
        public async Task StoreAsync_ThrowsWhenVirtualAECannotBeFound()
        {
            var correlationId = Guid.NewGuid().ToString();
            var service = new StowService(_serviceFactory.Object, _configuration);

            var httpRequest = new Mock<HttpRequest>();

            await Assert.ThrowsAsync<ApplicationEntityNotFoundException>(async () =>
                await service.StoreAsync(httpRequest.Object, "a.b.c.d", "aet", "workflow", correlationId, CancellationToken.None));
        }

        [Fact(DisplayName = "StoreAsync - Throws with bad StudyInstanceUID")]
        public async Task StoreAsync_ThrowsWithInvalidStudyInstanceUid()
        {
            var correlationId = Guid.NewGuid().ToString();
            var service = new StowService(_serviceFactory.Object, _configuration);
            var vae = new VirtualApplicationEntity
            {
                Name = Guid.NewGuid().ToString(),
                VirtualAeTitle = Guid.NewGuid().ToString(),
            };

            _repository.Setup(p => p.FindByAeTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(vae);

            var httpRequest = new Mock<HttpRequest>();

            await Assert.ThrowsAsync<DicomValidationException>(async () =>
                await service.StoreAsync(httpRequest.Object, "a.b.c.d", "aet", "workflow", correlationId, CancellationToken.None));
        }

        [Fact(DisplayName = "StoreAsync - Throws with bad content type header")]
        public async Task StoreAsync_ThrowsWithInvalidContentTypeHeader()
        {
            var correlationId = Guid.NewGuid().ToString();
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var service = new StowService(_serviceFactory.Object, _configuration);
            var vae = new VirtualApplicationEntity
            {
                Name = Guid.NewGuid().ToString(),
                VirtualAeTitle = Guid.NewGuid().ToString(),
            };

            _repository.Setup(p => p.FindByAeTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(vae);

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(p => p.Headers.ContentType).Returns(new StringValues("invalid-header"));

            await Assert.ThrowsAsync<UnsupportedContentTypeException>(async () =>
                await service.StoreAsync(httpRequest.Object, studyInstanceUid, "aet", "workflow", correlationId, CancellationToken.None));
        }

        [Fact(DisplayName = "StoreAsync - Throws with unsupported content type")]
        public async Task StoreAsync_ThrowsWithUnsupportedContentTypeHeader()
        {
            var correlationId = Guid.NewGuid().ToString();
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var service = new StowService(_serviceFactory.Object, _configuration);
            var vae = new VirtualApplicationEntity
            {
                Name = Guid.NewGuid().ToString(),
                VirtualAeTitle = Guid.NewGuid().ToString(),
            };

            _repository.Setup(p => p.FindByAeTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(vae);

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(p => p.ContentType).Returns(ContentTypes.ApplicationDicomJson);

            await Assert.ThrowsAsync<UnsupportedContentTypeException>(async () =>
                await service.StoreAsync(httpRequest.Object, studyInstanceUid, "aet", "workflow", correlationId, CancellationToken.None));
        }

        [Fact(DisplayName = "StoreAsync - handles single DICOM instance")]
        public async Task StoreAsync_HandlesSingleDicomInstance()
        {
            var correlationId = Guid.NewGuid().ToString();
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var service = new StowService(_serviceFactory.Object, _configuration);
            var vae = new VirtualApplicationEntity
            {
                Name = Guid.NewGuid().ToString(),
                VirtualAeTitle = Guid.NewGuid().ToString(),
            };

            _streamsWriter.Setup(p => p.Save(It.IsAny<IList<Stream>>(), It.IsAny<string>(), It.IsAny<VirtualApplicationEntity?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
            _repository.Setup(p => p.FindByAeTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(vae);

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(p => p.ContentType).Returns(ContentTypes.ApplicationDicom);
            httpRequest.SetupGet(p => p.HttpContext.Connection.RemoteIpAddress).Returns(IPAddress.Loopback);
            httpRequest.SetupGet(p => p.Body).Returns(Stream.Null);

            var exception = await Record.ExceptionAsync(async () => await service.StoreAsync(httpRequest.Object, studyInstanceUid, "aet", "workflow", correlationId, CancellationToken.None));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "StoreAsync - handles multiple DICOM instances")]
        public async Task StoreAsync_HandlesMultipleDicomInstances()
        {
            var correlationId = Guid.NewGuid().ToString();
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var service = new StowService(_serviceFactory.Object, _configuration);
            var vae = new VirtualApplicationEntity
            {
                Name = Guid.NewGuid().ToString(),
                VirtualAeTitle = Guid.NewGuid().ToString(),
            };

            _streamsWriter.Setup(p => p.Save(It.IsAny<IList<Stream>>(), It.IsAny<string>(), It.IsAny<VirtualApplicationEntity?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
            _repository.Setup(p => p.FindByAeTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(vae);

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(p => p.ContentType).Returns($"{ContentTypes.MultipartRelated}; boundary={Boundary}");
            httpRequest.SetupGet(p => p.HttpContext.Connection.RemoteIpAddress).Returns(IPAddress.Loopback);

            var body = await GenerateMultipartData();

            httpRequest.SetupGet(p => p.Body).Returns(new MemoryStream(body));

            var exception = await Record.ExceptionAsync(async () => await service.StoreAsync(httpRequest.Object, studyInstanceUid, "aet", "workflow", correlationId, CancellationToken.None));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "StoreAsync - handles multiple DICOM instances without virtual AE")]
        public async Task GivenMultipleDicomInstances_WhenStoreAsyncIsCalledWithoutVirtualAE_SavesStreams()
        {
            var correlationId = Guid.NewGuid().ToString();
            var studyInstanceUid = DicomUIDGenerator.GenerateDerivedFromUUID().UID;
            var service = new StowService(_serviceFactory.Object, _configuration);

            _streamsWriter.Setup(p => p.Save(It.IsAny<IList<Stream>>(), It.IsAny<string>(), It.IsAny<VirtualApplicationEntity?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(p => p.ContentType).Returns($"{ContentTypes.MultipartRelated}; boundary={Boundary}");
            httpRequest.SetupGet(p => p.HttpContext.Connection.RemoteIpAddress).Returns(IPAddress.Loopback);

            var body = await GenerateMultipartData();

            httpRequest.SetupGet(p => p.Body).Returns(new MemoryStream(body));

            var exception = await Record.ExceptionAsync(async () => await service.StoreAsync(httpRequest.Object, studyInstanceUid, null, "workflow", correlationId, CancellationToken.None));
            Assert.Null(exception);

            _repository.Verify(p => p.FindByAeTitleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        private async Task<byte[]> GenerateMultipartData()
        {
            using var stream = new MemoryStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"\r\n--{Boundary}\r\nContent-Type: {ContentTypes.ApplicationDicom}\r\n\r\n"));
            await stream.WriteAsync(Encoding.UTF8.GetBytes("data"));
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"\r\n--{Boundary}--"));
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }
    }
}
