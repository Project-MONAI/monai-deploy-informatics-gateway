// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Formatting.Parsers;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Maintains information about MIME body parts parsed by <see cref="MimeMultipartBodyPartParser"/>.
    /// </summary>
    internal class MimeBodyPart : IDisposable
    {
        private static readonly Type StreamType = typeof(Stream);
        private Stream _outputStream;
        private readonly MultipartStreamProvider _streamProvider;
        private HttpContent _parentContent;
        private HttpContent _content;
        private readonly HttpContentHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MimeBodyPart"/> class.
        /// </summary>
        /// <param name="streamProvider">The stream provider.</param>
        /// <param name="maxBodyPartHeaderSize">The max length of the MIME header within each MIME body part.</param>
        /// <param name="parentContent">The part's parent content</param>
        public MimeBodyPart(MultipartStreamProvider streamProvider, int maxBodyPartHeaderSize, HttpContent parentContent)
        {
            Contract.Assert(streamProvider != null);
            Contract.Assert(parentContent != null);
            _streamProvider = streamProvider;
            _parentContent = parentContent;
            Segments = new List<ArraySegment<byte>>(2);
            _headers = CreateEmptyContentHeaders();
            HeaderParser = new InternetMessageFormatHeaderParser(
                _headers,
                maxBodyPartHeaderSize,
                ignoreHeaderValidation: true);
        }

        /// <summary>
        /// Gets the header parser.
        /// </summary>
        /// <value>
        /// The header parser.
        /// </value>
        public InternetMessageFormatHeaderParser HeaderParser { get; private set; }

        /// <summary>
        /// Gets the part's content as an HttpContent.
        /// </summary>
        /// <value>
        /// The part's content, or null if the part had no content.
        /// </value>
        public HttpContent GetCompletedHttpContent()
        {
            Contract.Assert(IsComplete);

            if (_content == null)
            {
                return null;
            }

            _headers.CopyTo(_content.Headers);
            return _content;
        }

        /// <summary>
        /// Gets the set of <see cref="ArraySegment{T}"/> pointing to the read buffer with
        /// contents of this body part.
        /// </summary>
        public List<ArraySegment<byte>> Segments { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the body part has been completed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
        /// </value>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the final body part.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
        /// </value>
        public bool IsFinal { get; set; }

        /// <summary>
        /// Writes the <paramref name="segment"/> into the part's output stream.
        /// </summary>
        /// <param name="segment">The current segment to be written to the part's output stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public async Task WriteSegment(ArraySegment<byte> segment, CancellationToken cancellationToken)
        {
            var stream = GetOutputStream();
            await stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
        }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        /// <returns>The output stream to write the body part to.</returns>
        private Stream GetOutputStream()
        {
            if (_outputStream == null)
            {
                try
                {
                    _outputStream = _streamProvider.GetStream(_parentContent, _headers);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"The stream provider of type '{_streamProvider.GetType().Name}' threw an exception.", e);
                }

                if (_outputStream == null)
                {
                    throw new InvalidOperationException($"The stream provider of type '{_streamProvider.GetType().Name}' returned null. It must return a writable '{StreamType.Name}' instance.");
                }

                if (!_outputStream.CanWrite)
                {
                    throw new InvalidOperationException($"The stream provider of type '{_streamProvider.GetType().Name}' returned a read-only stream. It must return a writable '{StreamType.Name}' instance.");
                }
                _content = new StreamContent(_outputStream);
            }

            return _outputStream;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupOutputStream();
                CleanupHttpContent();
                _parentContent = null;
                HeaderParser = null;
                Segments.Clear();
            }
        }

        /// <summary>
        /// In the success case, the HttpContent is to be used after this Part has been parsed and disposed of.
        /// Only if Dispose has been called on a non-completed part, the parsed HttpContent needs to be disposed of as well.
        /// </summary>
        private void CleanupHttpContent()
        {
            if (!IsComplete && _content != null)
            {
                _content.Dispose();
            }

            _content = null;
        }

        /// <summary>
        /// Resets the output stream by either closing it or, in the case of a <see cref="MemoryStream"/> resetting
        /// position to 0 so that it can be read by the caller.
        /// </summary>
        private void CleanupOutputStream()
        {
            if (_outputStream != null)
            {
                MemoryStream output = _outputStream as MemoryStream;
                if (output != null)
                {
                    output.Position = 0;
                }
                else
                {
#if NETSTANDARD1_3
                    _outputStream.Dispose();
#else
                    _outputStream.Close();
#endif
                }

                _outputStream = null;
            }
        }

        /// <summary>
        /// Creates an empty <see cref="HttpContentHeaders"/> instance. The only way is to get it from a dummy
        /// <see cref="HttpContent"/> instance.
        /// </summary>
        /// <returns>The created instance.</returns>
        public static HttpContentHeaders CreateEmptyContentHeaders()
        {
            HttpContent tempContent = null;
            HttpContentHeaders contentHeaders = null;
            try
            {
                tempContent = new StringContent(String.Empty);
                contentHeaders = tempContent.Headers;
                contentHeaders.Clear();
            }
            finally
            {
                // We can dispose the content without touching the headers
                if (tempContent != null)
                {
                    tempContent.Dispose();
                }
            }

            return contentHeaders;
        }
    }
}
