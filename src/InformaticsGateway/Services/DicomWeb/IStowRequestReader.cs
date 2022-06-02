// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal interface IStowRequestReader
    {
        Task<IList<Stream>> GetStreams(HttpRequest request, MediaTypeHeaderValue mediaTypeHeaderValue, CancellationToken cancellationToken);
    }
}
