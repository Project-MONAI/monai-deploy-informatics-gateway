// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    /// <summary>
    /// IStowService provides APIs to store DICOM instances to a remote
    /// DICOMweb server.
    /// This client does not transcode the input data; all input DICOM  dataset
    /// are transfered as-is using the stored Transfer Syntax.
    /// </summary>
    public interface IStowService : IServiceBase
    {
        /// <summary>
        /// Stores all DICOM files to the remote DICOMweb server.
        /// </summary>
        /// <param name="dicomFiles">DICOM files to be stored.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<DicomWebResponse<string>> Store(IEnumerable<DicomFile> dicomFiles, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores all DICOM files for the specified Study Instance UID.
        /// Note: any files found not matching the specified <c>studyInstanceUid</c> may be rejected.
        /// </summary>
        /// <param name="studyInstanceUid">Study Instance UID</param>
        /// <param name="dicomFiles">DICOM files to be stored.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<DicomWebResponse<string>> Store(string studyInstanceUid, IEnumerable<DicomFile> dicomFiles, CancellationToken cancellationToken = default);
    }
}
