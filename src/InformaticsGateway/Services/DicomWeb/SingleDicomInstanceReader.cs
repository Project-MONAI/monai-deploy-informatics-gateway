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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal class SingleDicomInstanceReader : DicomInstanceReaderBase, IStowRequestReader
    {
        public SingleDicomInstanceReader(InformaticsGatewayConfiguration configuration, ILogger<SingleDicomInstanceReader> logger, IFileSystem fileSystem)
            : base(configuration, logger, fileSystem)
        {
        }

        public async Task<IList<Stream>> GetStreams(HttpRequest request, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.Null(mediaTypeHeaderValue);

            try
            {
                var streams = new List<Stream>
                {
                    await ConvertStream(request.HttpContext, request.Body, cancellationToken).ConfigureAwait(false)
                };
                return streams;
            }
            catch (Exception ex)
            {
                throw new ConvertStreamException("Error converting data stream.", ex);
            }
        }
    }
}
