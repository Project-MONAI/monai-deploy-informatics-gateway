/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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

using Microsoft.Extensions.Configuration;
using Monai.Deploy.Storage.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class StorageConfiguration : StorageServiceConfiguration
    {
        /// <summary>
        /// Gets or sets whether to store temporary data in <c>Memory</c> or on <c>Disk</c>.
        /// Defaults to <c>Memory</c>.
        /// </summary>
        [ConfigurationKeyName("tempStorageLocation")]
        public TemporaryDataStorageLocation TemporaryDataStorage { get; set; } = TemporaryDataStorageLocation.Disk;

        /// <summary>
        /// Gets or sets the path used for buffering incoming data.
        /// Defaults to <c>./temp</c>.
        /// </summary>
        [ConfigurationKeyName("localTemporaryStoragePath")]
        public string LocalTemporaryStoragePath { get; set; } = "/payloads";

        /// <summary>
        /// Gets or sets the number of bytes buffered for reads and writes to the temporary file.
        /// Defaults to <c>128000</c>.
        /// </summary>
        [ConfigurationKeyName("bufferSize")]
        public int BufferSize { get; set; } = 128000;

        /// <summary>
        /// Gets or set the maximum memory buffer size in bytes with default to 30MiB.
        /// </summary>
        [ConfigurationKeyName("memoryThreshold")]
        public int MemoryThreshold { get; set; } = 31457280;

        /// <summary>
        /// Gets or sets the name of the bucket where payloads are uploaded to.
        /// </summary>
        [ConfigurationKeyName("bucketName")]
        public string StorageServiceBucketName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the bucket used for storing objects before they are assembled into payloads.
        /// </summary>
        [ConfigurationKeyName("temporaryBucketName")]
        public string TemporaryStorageBucket { get; set; }

        /// <summary>
        /// Gets or sets root directory path for storing incoming data in the <c>temporaryBucketName</c>.
        /// Defaults to <c>/incoming</c>.
        /// </summary>
        [ConfigurationKeyName("remoteTemporaryStoragePath")]
        public string RemoteTemporaryStoragePath { get; set; } = "/incoming";

        /// <summary>
        /// Gets or sets the watermark for disk usage with default value of 75%,
        /// meaning that MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retrieving data via DICOMweb when used disk space
        /// is above the watermark.
        /// </summary>
        [ConfigurationKeyName("watermarkPercent")]
        public uint Watermark { get; set; } = 75;

        /// <summary>
        /// Gets or sets the reserved disk space for the MONAI Deploy Informatics Gateway with default value of 5GB.
        /// MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retrieving data via DICOMweb when available disk space
        /// is less than the value.
        /// </summary>
        [ConfigurationKeyName("reserveSpaceGB")]
        public uint ReserveSpaceGB { get; set; } = 5;

        /// <summary>
        /// Gets or sets retry options relate to saving files to temporary storage, processing payloads and uploading payloads to the storage service.
        /// </summary>
        [ConfigurationKeyName("retries")]
        public RetryConfiguration Retries { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Gets or set number of payloads to be processed at a given time. Default is 1;
        /// </summary>
        [ConfigurationKeyName("payloadProcessThreads")]
        public int PayloadProcessThreads { get; set; } = 1;

        /// <summary>
        /// Gets or set the maximum number of concurrent uploads. Default is 2;
        /// </summary>
        [ConfigurationKeyName("concurrentUploads")]
        public int ConcurrentUploads { get; set; } = 2;

        /// <summary>
        /// Gets or set the timeout value, in milliseconds, for calls made to the storage service. Default is 5000;
        /// This applies to the following calls: ListObjectsAsync, VerifyObjectsExistAsync, VerifyObjectExistsAsync, ListObjectsWithCredentialsAsync.
        /// </summary>
        [ConfigurationKeyName("storageServiceListTimeout")]
        public int StorageServiceListTimeout { get; set; } = 5000;
    }
}
