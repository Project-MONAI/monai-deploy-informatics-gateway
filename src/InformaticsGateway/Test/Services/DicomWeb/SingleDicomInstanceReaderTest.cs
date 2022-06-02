// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO;
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
        private readonly DicomWebConfiguration _dicomWebConfiguration;
        private readonly Mock<ILogger<SingleDicomInstanceReader>> _logger;

        public SingleDicomInstanceReaderTest()
        {
            _dicomWebConfiguration = new DicomWebConfiguration();
            _logger = new Mock<ILogger<SingleDicomInstanceReader>>();
        }

        [Fact(DisplayName = "GetStreams - throws ConvertStreamException on error")]
        public async Task GetStreams_ThrowsConvertStreamExceptionOnError()
        {
            var httpContext = new DefaultHttpContext();
            var reader = new SingleDicomInstanceReader(_dicomWebConfiguration, _logger.Object);
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
            var reader = new SingleDicomInstanceReader(_dicomWebConfiguration, _logger.Object);
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
            var reader = new SingleDicomInstanceReader(_dicomWebConfiguration, _logger.Object);
            var contentType = new MediaTypeHeaderValue(ContentTypes.ApplicationDicom);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            request.SetupGet(p => p.Body).Returns(Stream.Null);
            var result = await reader.GetStreams(request.Object, contentType, CancellationToken.None);
            Assert.Single(result);
        }
    }
}
