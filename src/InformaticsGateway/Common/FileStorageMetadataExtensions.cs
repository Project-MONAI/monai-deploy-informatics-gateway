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

            dicomFileStorageMetadata.JsonFile.Data = new MemoryStream(Encoding.UTF8.GetBytes(dicomJson));
            dicomFileStorageMetadata.JsonFile.Data.Seek(0, SeekOrigin.Begin);
        }

        public static async Task SetDataStream(this FhirFileStorageMetadata fhirFileStorageMetadata, string json)
        {
            Guard.Against.Null(json, nameof(json)); // allow empty here

            fhirFileStorageMetadata.File.Data = new MemoryStream();
            var sw = new StreamWriter(fhirFileStorageMetadata.File.Data, Encoding.UTF8);
            await sw.WriteAsync(json).ConfigureAwait(false);
            await sw.FlushAsync().ConfigureAwait(false);
            fhirFileStorageMetadata.File.Data.Seek(0, SeekOrigin.Begin);
        }
    }
}
