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

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.DicomWeb
{
    public class SingleDicomInstanceReaderTest
    {
        private readonly InformaticsGatewayConfiguration _configuration;
        private readonly Mock<ILogger<SingleDicomInstanceReader>> _logger;
        private readonly MockFileSystem _fileSystem;

        public SingleDicomInstanceReaderTest()
        {
            _configuration = new InformaticsGatewayConfiguration();
            _logger = new Mock<ILogger<SingleDicomInstanceReader>>();
            _fileSystem = new MockFileSystem();
        }

        [Fact(DisplayName = "GetStreams - throws ConvertStreamException on error")]
        public async Task GetStreams_ThrowsConvertStreamExceptionOnError()
        {
            var httpContext = new DefaultHttpContext();
            var reader = new SingleDicomInstanceReader(_configuration, _logger.Object, _fileSystem);
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationDicom);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            await Assert.ThrowsAsync<ConvertStreamException>(async () => await reader.GetStreams(request.Object, contentType, CancellationToken.None));
        }

        [Fact(DisplayName = "GetStreams - buffers stream using disk")]
        public async Task GetStreams_BuffersStreamWithDisk()
        {
            var httpContext = new DefaultHttpContext();
            var nonSeekableStream = new Mock<Stream>();
            nonSeekableStream.SetupGet(p => p.CanSeek).Returns(false);
            var reader = new SingleDicomInstanceReader(_configuration, _logger.Object, _fileSystem);
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationDicom);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            request.SetupGet(p => p.Body).Returns(nonSeekableStream.Object);
            var exception = await Record.ExceptionAsync(async () => await reader.GetStreams(request.Object, contentType, CancellationToken.None));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "GetStreams - use original stream")]
        public async Task GetStreams_UseOriginalRequestStream()
        {
            var httpContext = new DefaultHttpContext();
            var reader = new SingleDicomInstanceReader(_configuration, _logger.Object, _fileSystem);
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationDicom);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            request.SetupGet(p => p.Body).Returns(Stream.Null);
            var result = await reader.GetStreams(request.Object, contentType, CancellationToken.None);
            Assert.Single(result);
        }
    }
}
