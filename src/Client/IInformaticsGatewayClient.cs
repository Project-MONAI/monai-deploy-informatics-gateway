// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Client.Services;

namespace Monai.Deploy.InformaticsGateway.Client
{
    public interface IInformaticsGatewayClient
    {
        /// <summary>
        /// Provides Health service to query service status, liveness and readiness.
        /// </summary>
        IHealthService Health { get; }

        /// <summary>
        /// Provides APIs to create a new inference request or query status of an existing inference request.
        /// </summary>
        IInferenceService Inference { get; }

        /// <summary>
        /// Provides APIs to list, create, delete MONAI Deploy SCP AE Titles.
        /// </summary>
        IAeTitleService<MonaiApplicationEntity> MonaiScpAeTitle { get; }

        /// <summary>
        /// Provides APIs to list, create, delete DICOM sources.
        /// </summary>
        IAeTitleService<SourceApplicationEntity> DicomSources { get; }

        /// <summary>
        /// Provides APIs to list, create, delete DICOM destinations.
        /// </summary>
        IAeTitleService<DestinationApplicationEntity> DicomDestinations { get; }

        /// <summary>
        /// Configures the service URI of the DICOMweb service.
        /// </summary>
        /// <param name="uriRoot">Base URL of the DICOMweb server.</param>
        void ConfigureServiceUris(Uri uriRoot);

        /// <summary>
        /// Configures the authentication header for the Informatics Gateway client.
        /// </summary>
        /// <param name="value"></param>
        void ConfigureAuthentication(AuthenticationHeaderValue value);
    }
}
