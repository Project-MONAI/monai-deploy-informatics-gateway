// Copyright 2021-2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;
using System;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.Services.Scp
{
    public interface IApplicationEntityManager
    {
        IOptions<InformaticsGatewayConfiguration> Configuration { get; }

        /// <summary>
        /// Gets whether MONAI Deploy Informatics Gateway can handle C-Store requests.
        /// </summary>
        /// <value></value>
        bool CanStore { get; }

        /// <summary>
        /// Handles the C-Store request.
        /// </summary>
        /// <param name="request">Instance of <see cref="Dicom.Network.DicomCStoreRequest" />.</param>
        /// <param name="calledAeTitle">Called AE Title to be associated with the call.</param>
        /// <param name="calledAeTitle">Calling AE Title to be associated with the call.</param>
        /// <param name="associationId">Unique association ID.</param>
        Task HandleCStoreRequest(DicomCStoreRequest request, string calledAeTitle, string callingAeTitle, Guid associationId);

        /// <summary>
        /// Checks if a MONAI AET is configured.
        /// </summary>
        /// <param name="calledAe"></param>
        /// <returns>True if the AE Title is configured; false otherwise.</returns>
        bool IsAeTitleConfigured(string calledAe);

        /// <summary>
        /// Wrapper to get injected service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        T GetService<T>();

        /// <summary>
        /// Wrapper to get a typed logger.
        /// </summary>
        ILogger GetLogger(string calledAeTitle);

        /// <summary>
        /// Checks if source AE Title is configured.
        /// </summary>
        /// <param name="callingAe"></param>
        /// <returns></returns>
        bool IsValidSource(string callingAe, string host);
    }
}
