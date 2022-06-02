// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal class MultipartDicomInstanceReader : DicomInstanceReaderBase, IStowRequestReader
    {
        public MultipartDicomInstanceReader(DicomWebConfiguration dicomWebConfiguration, ILogger<MultipartDicomInstanceReader> logger)
            : base(dicomWebConfiguration, logger)
        {
        }

        public async Task<IList<Stream>> GetStreams(HttpRequest request, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.Null(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue));

            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeaderValue.Boundary).ToString();

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new UnsupportedContentTypeException("Content type header must include a valid value for boundary.");
            }

            if (mediaTypeHeaderValue.Parameters is not null)
            {
                foreach (var parameter in mediaTypeHeaderValue.Parameters)
                {
                    if (parameter.Name.Equals(SR.TypeParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        var rootContentType = HeaderUtilities.RemoveQuotes(parameter.Value).ToString();
                        ValidateSupportedMediaTypes(rootContentType, out var _, ContentTypes.ApplicationDicom);
                    }
                    else if (parameter.Name.Equals(SR.BoundaryParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    else
                    {
                        Logger.MultipartParameterIgnored(parameter.Name.ToString(), parameter.Value.ToString());
                    }
                }
            }

            try
            {
                var multipartReader = new MultipartReader(boundary, request.Body)
                {
                    BodyLengthLimit = Configuration.MaxAllowedFileSize
                };

                var streams = new List<Stream>();
                MultipartSection multipartSection;
                while ((multipartSection = await multipartReader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false)) is not null)
                {
                    var contentType = multipartSection.ContentType;

                    if (!string.IsNullOrWhiteSpace(contentType))
                    {
                        ValidateSupportedMediaTypes(contentType, out var _, ContentTypes.ApplicationDicom);
                    }
                    streams.Add(await ConvertStream(request.HttpContext, multipartSection.Body, cancellationToken).ConfigureAwait(false));
                }
                return streams;
            }
            catch (Exception ex)
            {
                throw new ConvertStreamException("Error converting data stream.", ex);
            }
        }
    }
}
