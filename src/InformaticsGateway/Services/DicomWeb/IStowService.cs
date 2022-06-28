// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Monai.Deploy.InformaticsGateway.Services.DicomWeb
{
    internal interface IStowService
    {
        Task<StowResult> StoreAsync(HttpRequest request, string studyInstanceUid, string workflowName, string correlationId, CancellationToken cancellationToken);
    }
}
