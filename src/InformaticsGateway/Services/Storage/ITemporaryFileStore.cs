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
