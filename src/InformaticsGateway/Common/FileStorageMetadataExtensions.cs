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

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Common
{
    internal static class FileStorageMetadataExtensions
    {
        public static async Task SetDataStreams(this DicomFileStorageMetadata dicomFileStorageMetadata, DicomFile dicomFile, string dicomJson)
        {
            Guard.Against.Null(dicomFile, nameof(dicomFile));
            Guard.Against.Null(dicomJson, nameof(dicomJson)); // allow empty here

            dicomFileStorageMetadata.File.Data = new MemoryStream();
            await dicomFile.SaveAsync(dicomFileStorageMetadata.File.Data).ConfigureAwait(false);
            dicomFileStorageMetadata.File.Data.Seek(0, SeekOrigin.Begin);

            SetTextStream(dicomFileStorageMetadata.JsonFile, dicomJson);
        }

        public static void SetDataStream(this FhirFileStorageMetadata fhirFileStorageMetadata, string json)
            => SetTextStream(fhirFileStorageMetadata.File, json);

        public static void SetDataStream(this Hl7FileStorageMetadata hl7FileStorageMetadata, string message)
            => SetTextStream(hl7FileStorageMetadata.File, message);

        private static void SetTextStream(StorageObjectMetadata storageObjectMetadata, string message)
        {
            Guard.Against.Null(message, nameof(message)); // allow empty here

            storageObjectMetadata.Data = new MemoryStream(Encoding.UTF8.GetBytes(message));
            storageObjectMetadata.Data.Seek(0, SeekOrigin.Begin);
        }
    }
}
