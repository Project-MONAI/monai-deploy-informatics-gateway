/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Net.Http.Headers;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Models;
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
        /// Provides APIs to list, create, delete Virtual AE Titles.
        /// </summary>
        IAeTitleService<VirtualApplicationEntity> VirtualAeTitle { get; }

        /// <summary>
        /// Provides APIs to list, create, delete Virtual AE Titles.
        /// </summary>
        IAeTitleService<HL7DestinationEntity> HL7Destinations { get; }

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
