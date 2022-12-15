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

using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Common
{
    internal static class FileStorageMetadataExtensions
    {
        public static async Task SetDataStreams(
            this DicomFileStorageMetadata dicomFileStorageMetadata,
            DicomFile dicomFile,
            string dicomJson,
            TemporaryDataStorageLocation storageLocation,
            IFileSystem fileSystem = null,
            string temporaryStoragePath = "")
        {
            Guard.Against.Null(dicomFile);
            Guard.Against.Null(dicomJson); // allow empty here

            switch (storageLocation)
            {
                case TemporaryDataStorageLocation.Disk:
                    Guard.Against.Null(fileSystem);
                    Guard.Against.NullOrWhiteSpace(temporaryStoragePath);

                    var tempFile = fileSystem.Path.Combine(temporaryStoragePath, $@"{fileSystem.Path.GetRandomFileName()}");
                    dicomFileStorageMetadata.File.Data = fileSystem.File.Create(tempFile);
                    break;

                default:
                    dicomFileStorageMetadata.File.Data = new System.IO.MemoryStream();
                    break;
            }

            await dicomFile.SaveAsync(dicomFileStorageMetadata.File.Data).ConfigureAwait(false);
            dicomFileStorageMetadata.File.Data.Seek(0, System.IO.SeekOrigin.Begin);

            await SetTextStream(dicomFileStorageMetadata.JsonFile, dicomJson, storageLocation, fileSystem, temporaryStoragePath).ConfigureAwait(false);
        }

        public static async Task SetDataStream(
            this FhirFileStorageMetadata fhirFileStorageMetadata,
            string json,
            TemporaryDataStorageLocation storageLocation,
            IFileSystem fileSystem = null,
            string temporaryStoragePath = "")
            => await SetTextStream(fhirFileStorageMetadata.File, json, storageLocation, fileSystem, temporaryStoragePath).ConfigureAwait(false);

        public static async Task SetDataStream(
            this Hl7FileStorageMetadata hl7FileStorageMetadata,
             string message,
             TemporaryDataStorageLocation storageLocation,
            IFileSystem fileSystem = null,
             string temporaryStoragePath = "")
            => await SetTextStream(hl7FileStorageMetadata.File, message, storageLocation, fileSystem, temporaryStoragePath).ConfigureAwait(false);

        private static async Task SetTextStream(
            StorageObjectMetadata storageObjectMetadata,
            string message,
            TemporaryDataStorageLocation storageLocation,
            IFileSystem fileSystem = null,
            string temporaryStoragePath = "")
        {
            Guard.Against.Null(message); // allow empty here

            switch (storageLocation)
            {
                case TemporaryDataStorageLocation.Disk:
                    Guard.Against.Null(fileSystem);
                    Guard.Against.NullOrWhiteSpace(temporaryStoragePath);

                    var tempFile = fileSystem.Path.Combine(temporaryStoragePath, $@"{fileSystem.Path.GetRandomFileName()}");
                    var stream = fileSystem.File.Create(tempFile);
                    var data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                    storageObjectMetadata.Data = stream;
                    break;

                default:
                    storageObjectMetadata.Data = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(message));
                    break;
            }

            storageObjectMetadata.Data.Seek(0, System.IO.SeekOrigin.Begin);
        }
    }
}
