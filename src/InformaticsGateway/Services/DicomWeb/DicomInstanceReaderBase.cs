﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
        private const string BufferDirectoryName = "IGTEMP";
        protected DicomWebConfiguration Configuration { get; }
        protected ILogger Logger { get; }
        protected IFileSystem FileSystem { get; }

        protected DicomInstanceReaderBase(
            DicomWebConfiguration dicomWebConfiguration,
            ILogger logger,
            IFileSystem fileSystem)
        {
            Configuration = dicomWebConfiguration ?? throw new ArgumentNullException(nameof(dicomWebConfiguration));
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
                var tempPath = FileSystem.Path.Combine(Path.GetTempPath(), BufferDirectoryName);

                lock (SyncLock)
                {
                    FileSystem.Directory.CreateDirectoryIfNotExists(tempPath);
                }

                Logger.ConvertingStreamToFileBufferingReadStream(Configuration.MemoryThreshold, tempPath);
                seekableStream = new FileBufferingReadStream(sourceStream, Configuration.MemoryThreshold, null, tempPath);
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
