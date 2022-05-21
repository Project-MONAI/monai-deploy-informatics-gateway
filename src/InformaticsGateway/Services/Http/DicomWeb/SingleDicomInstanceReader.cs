// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Services.Http.DicomWeb
{
    internal class SingleDicomInstanceReader : DicomInstanceReaderBase, IStowRequestReader
    {
        public SingleDicomInstanceReader(DicomWebConfiguration dicomWebConfiguration, ILogger<SingleDicomInstanceReader> logger)
            : base(dicomWebConfiguration, logger)
        {
        }

        public async Task<IList<Stream>> GetStreams(HttpRequest request, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));
            Guard.Against.Null(mediaTypeHeaderValue, nameof(mediaTypeHeaderValue));

            var streams = new List<Stream>
            {
                await ConvertStream(request.HttpContext, request.Body, cancellationToken).ConfigureAwait(false)
            };
            return streams;
        }
    }
}
