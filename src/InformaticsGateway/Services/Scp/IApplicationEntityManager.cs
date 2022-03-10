// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Configuration;

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
