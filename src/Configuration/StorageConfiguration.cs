// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.IO.Abstractions;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Monai.Deploy.Storage.Configuration;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class StorageConfiguration : StorageServiceConfiguration
    {
        private readonly IFileSystem _fileSystem;

        public StorageConfiguration() : this(new FileSystem())
        { }

        public StorageConfiguration(IFileSystem fileSystem) => _fileSystem = fileSystem ?? throw new System.ArgumentNullException(nameof(fileSystem));

        /// <summary>
        /// Gets or sets temporary storage path.
        /// This is used to store all instances received to a temporary folder.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("temporary")]
        public string Temporary { get; set; } = "./payloads";

        /// <summary>
        /// Gets or sets the watermark for disk usage with default value of 75%,
        /// meaning that MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retreiving data via DICOMweb when used disk space
        /// is above the watermark.
        /// </summary>
        /// <value></value>
        [ConfigurationKeyName("watermarkPercent")]
        public uint Watermark { get; set; } = 75;

        /// <summary>
        /// Gets or sets the reserved disk space for the MONAI Deploy Informatics Gateway with default value of 5GB.
        /// MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retreiving data via DICOMweb when available disk space
        /// is less than the value.
        /// </summary>
        /// <value></value>
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

        [JsonIgnore]
        public string TemporaryDataDirFullPath
        {
            get
            {
                return _fileSystem.Path.GetFullPath(Temporary);
            }
        }

        /// <summary>
        /// Gets or sets the name of the bucket where payloads are uploaded to.
        /// </summary>
        [ConfigurationKeyName("bucketName")]
        public string StorageServiceBucketName { get; set; } = string.Empty;
    }
}
