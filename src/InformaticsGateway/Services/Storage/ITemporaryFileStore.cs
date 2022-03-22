// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Common;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal interface ITemporaryFileStore
    {
        /// <summary>
        /// Saves a DICOM instances to the temporary file store.
        /// </summary>
        /// <param name="transactionId">The association/transaction ID associated with the file.</param>
        /// <param name="file">Instance of DIcomFile to be stored.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Instance of DicomFileStorageInfo.</returns>
        Task<DicomStoragePaths> SaveDicomInstance(string transactionId, DicomFile dicomFile, CancellationToken cancellationToken);

        /// <summary>
        /// Saves a FHIR resource to the temporary file store.
        /// </summary>
        /// <param name="transactionId">The association/transaction ID associated with the file.</param>
        /// <param name="resourceType">The FHIR resource type.</param>
        /// <param name="resourceId">The FHIR resource ID.</param>
        /// <param name="data">Data content of the FHIR resource.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Instance of FhirFileStorageInfo.</returns>
        Task<FhirStoragePath> SaveFhirResource(string transactionId, string resourceType, string resourceId, FhirStorageFormat fhirFormat, string data, CancellationToken cancellationToken);

        /// <summary>
        /// Restores previously retrieved files for an inference request.
        /// </summary>
        /// <param name="transactionId">The transaction ID of the inference request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of FileSotragePath objects.</returns>
        IReadOnlyList<FileStoragePath> RestoreInferenceRequestFiles(string transactionId, CancellationToken cancellationToken);
    }
}
