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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal abstract class DicomInstanceReaderBase
    {
        private static readonly object SyncLock = new();
        protected InformaticsGatewayConfiguration Configuration { get; }
        protected ILogger Logger { get; }
        protected IFileSystem FileSystem { get; }

        protected DicomInstanceReaderBase(
            InformaticsGatewayConfiguration configuration,
            ILogger logger,
            IFileSystem fileSystem)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            FileSystem = fileSystem;
        }

        protected static void ValidateSupportedMediaTypes(string contentType, out MediaTypeHeaderValue mediaTypeHeaderValue, params string[] contentTypes)
        {
            Guard.Against.Null(contentType, nameof(contentType));

            if (MediaTypeHeaderValue.TryParse(contentType, out var mediaType) &&
                contentTypes.Any(p => p.Equals(mediaType.MediaType.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                mediaTypeHeaderValue = mediaType;
                return;
            }

            throw new UnsupportedContentTypeException($"The content type of '{contentType}' is not supported.");
        }

        protected async Task<Stream> ConvertStream(HttpContext httpContext, Stream sourceStream, CancellationToken cancellationToken = default)
        {
            Guard.Against.Null(httpContext, nameof(httpContext));
            Guard.Against.Null(sourceStream, nameof(sourceStream));

            Stream seekableStream;
            if (!sourceStream.CanSeek)
            {
                lock (SyncLock)
                {
                    FileSystem.Directory.CreateDirectoryIfNotExists(Configuration.Storage.BufferStorageRootPath);
                }

                Logger.ConvertingStreamToFileBufferingReadStream(Configuration.Storage.MemoryThreshold, Configuration.Storage.BufferStorageRootPath);
                seekableStream = new FileBufferingReadStream(sourceStream, Configuration.Storage.MemoryThreshold, null, Configuration.Storage.BufferStorageRootPath);
                httpContext.Response.RegisterForDisposeAsync(seekableStream);
                await seekableStream.DrainAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                seekableStream = sourceStream;
            }

            seekableStream.Seek(0, SeekOrigin.Begin);

            return seekableStream;
        }
    }
}
