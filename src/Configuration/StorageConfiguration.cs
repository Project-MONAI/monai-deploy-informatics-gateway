// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Newtonsoft.Json;
using System.IO.Abstractions;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public class StorageConfiguration
    {
        private readonly IFileSystem _fileSystem;

        public StorageConfiguration() : this(new FileSystem())
        {
        }

        public StorageConfiguration(IFileSystem fileSystem)
            => _fileSystem = fileSystem ?? throw new System.ArgumentNullException(nameof(fileSystem));

        /// <summary>
        /// Gets or sets temporary storage path.
        /// This is used to store all instances received to a temporary folder.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "temporary")]
        public string Temporary { get; set; } = "./payloads";

        /// <summary>
        /// Gets or sets the watermark for disk usage with default value of 75%,
        /// meaning that MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retreiving data via DICOMweb when used disk space
        /// is above the watermark.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "watermarkPercent")]
        public uint Watermark { get; set; } = 75;

        /// <summary>
        /// Gets or sets the reserved disk space for the MONAI Deploy Informatics Gateway with default value of 5GB.
        /// MONAI Deploy Informatics Gateway will stop accepting (C-STORE-RQ) associations,
        /// stop exporting and stop retreiving data via DICOMweb when available disk space
        /// is less than the value.
        /// </summary>
        /// <value></value>
        [JsonProperty(PropertyName = "reserveSpaceGB")]
        public uint ReserveSpaceGB { get; set; } = 5;

        /// <summary>
        /// Gets or sets the a fully qualified type name of the storage service.
        /// The spcified type must implement <typeparam name="Monai.Deploy.InformaticsGateway.Api.Storage.IStorageService">IStorageService</typeparam> interface.
        /// The default storage service configured is MinIO.
        /// </summary>

        [JsonProperty(PropertyName = "storageService")]
        public string StorageService { get; set; } = "Monai.Deploy.InformaticsGateway.Storage.MinIoStorageService, Monai.Deploy.InformaticsGateway.Storage.MinIo";

        /// <summary>
        /// Gets or sets credentials used to access the storage service.
        /// </summary>
        [JsonProperty(PropertyName = "storageServiceCredentials")]
        public ServiceCredentials StorageServiceCredentials { get; set; }

        /// <summary>
        /// Gets or sets retry options relate to processing payloads and uploading payloads to the storage service.
        /// </summary>
        [JsonProperty(PropertyName = "reties")]
        public RetryConfiguration Retries { get; set; } = new RetryConfiguration();

        /// <summary>
        /// Gets or set whether to use secured connection to the storage service.  Default is true.
        /// </summary>
        [JsonProperty(PropertyName = "securedConnection")]
        public bool SecuredConnection { get; set; } = true;

        /// <summary>
        /// Gets or set number of threads used for payload upload. Default is 1;
        /// </summary>
        public int Concurrentcy { get; set; } = 1;

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
        [JsonProperty(PropertyName = "storageServiceBucketName")]
        public string StorageServiceBucketName { get; set; }
    }
}
