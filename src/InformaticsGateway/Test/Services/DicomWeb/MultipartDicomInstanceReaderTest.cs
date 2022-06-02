// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Services.DicomWeb;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.DicomWeb
{
    public class MultipartDicomInstanceReaderTest
    {
        private const string Boundary = "BOUNDARY";
        private readonly DicomWebConfiguration _dicomWebConfiguration;
        private readonly Mock<ILogger<MultipartDicomInstanceReader>> _logger;

        public MultipartDicomInstanceReaderTest()
        {
            _dicomWebConfiguration = new DicomWebConfiguration();
            _logger = new Mock<ILogger<MultipartDicomInstanceReader>>();
        }

        [Fact(DisplayName = "GetStreams - throws UnsupportedContentTypeException with unsupported content type")]
        public async Task GetStreams_ThrowsUnsupportedContentTypeExceptionWithUnsupportedContentType()
        {
            var httpContext = new DefaultHttpContext();
            var reader = new MultipartDicomInstanceReader(_dicomWebConfiguration, _logger.Object);
            var contentType = CreateMultipartHeader(ContentTypes.ApplicationDicomXml);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            await Assert.ThrowsAsync<UnsupportedContentTypeException>(async () => await reader.GetStreams(request.Object, contentType, CancellationToken.None));
        }

        [Fact(DisplayName = "GetStreams - returns multiple streams")]
        public async Task GetStreams_ReturnsMultipleStreams()
        {
            var httpContext = new DefaultHttpContext();
            var reader = new MultipartDicomInstanceReader(_dicomWebConfiguration, _logger.Object);
            var contentType = CreateMultipartHeader(ContentTypes.ApplicationDicom);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            request.SetupGet(p => p.Body).Returns(new MemoryStream(await GenerateMultipartData()));

            var result = await reader.GetStreams(request.Object, contentType, CancellationToken.None);
            Assert.Equal(3, result.Count);
        }

        [Fact(DisplayName = "GetStreams - throws ConvertStreamException on error")]
        public async Task GetStreams_ThrowsConvertStreamExceptionOnError()
        {
            var httpContext = new DefaultHttpContext();
            var reader = new MultipartDicomInstanceReader(_dicomWebConfiguration, _logger.Object);
            var contentType = CreateMultipartHeader(ContentTypes.ApplicationDicom);
            var request = new Mock<HttpRequest>();
            request.SetupGet(p => p.HttpContext).Returns(httpContext);
            await Assert.ThrowsAsync<ConvertStreamException>(async () => await reader.GetStreams(request.Object, contentType, CancellationToken.None));
        }

        private async Task<byte[]> GenerateMultipartData()
        {
            using var stream = new MemoryStream();
            for (var i = 0; i < 3; i++)
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes($"\r\n--{Boundary}\r\nContent-Type: {ContentTypes.ApplicationDicom}\r\n\r\n"));
                await stream.WriteAsync(Encoding.UTF8.GetBytes("data"));
            }
            await stream.WriteAsync(Encoding.UTF8.GetBytes($"\r\n--{Boundary}--"));
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        private MediaTypeHeaderValue CreateMultipartHeader(string innerContentType)
        {
            var header = new MediaTypeHeaderValue(ContentTypes.MultipartRelated);
            header.Parameters.Add(new NameValueHeaderValue(SR.TypeParameterName, $"\"{innerContentType}\""));
            header.Parameters.Add(new NameValueHeaderValue(SR.BoundaryParameterName, Boundary));
            return header;
        }
    }
}
