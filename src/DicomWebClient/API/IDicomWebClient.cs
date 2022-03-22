// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net.Http.Headers;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public enum DicomWebServiceType
    {
        /// <summary>
        /// DICOMweb Query Service
        /// </summary>
        Qido,

        /// <summary>
        /// DICOMweb Retrieve Service
        /// </summary>
        Wado,

        /// <summary>
        /// DICOMweb Store Service
        /// </summary>
        Stow,

        /// <summary>
        /// DICOMweb Delete Service
        /// </summary>
        Delete
    }

    /// <summary>
    /// Interface for the DICOMweb client.
    /// </summary>
    public interface IDicomWebClient
    {
        /// <summary>
        /// Provides DICOMweb WADO services for retrieving studies, series, instances, frames and bulkdata.
        /// </summary>
        IWadoService Wado { get; }

        /// <summary>
        /// Provides DICOMweb QIDO services for querying a remote server for studies.
        /// </summary>
        IQidoService Qido { get; }

        /// <summary>
        /// Provides DICOMweb STOW services for storing DICOM instances.
        /// </summary>
        IStowService Stow { get; }

        /// <summary>
        /// Configures the service URI of the DICOMweb service.
        /// </summary>
        /// <param name="uriRoot">Base URL of the DICOMweb server.</param>
        void ConfigureServiceUris(Uri uriRoot);

        /// <summary>
        /// Configures prefix for the specified service
        /// </summary>
        /// <param name="serviceType"><c>ServiceType</c> to be configured</param>
        /// <param name="urlPrefix">Url prefix</param>
        void ConfigureServicePrefix(DicomWebServiceType serviceType, string urlPrefix);

        /// <summary>
        /// Configures the authentication header for the DICOMweb client.
        /// </summary>
        /// <param name="value"></param>
        void ConfigureAuthentication(AuthenticationHeaderValue value);
    }
}
