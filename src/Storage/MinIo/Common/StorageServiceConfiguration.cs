// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using Microsoft.Extensions.Configuration;

namespace Monai.Deploy.Storage.Common
{
    public class StorageServiceConfiguration
    {
        /// <summary>
        /// Gets or sets the a fully qualified type name of the storage service.
        /// The specified type must implement <typeparam name="Monai.Deploy.Storage.IStorageService">IStorageService</typeparam> interface.
        /// The default storage service configured is MinIO.
        /// </summary>
        [ConfigurationKeyName("serviceAssemblyName")]
        public string ServiceAssemblyName { get; set; } = "Monai.Deploy.Storage.MinIo.MinIoStorageService, Monai.Deploy.Storage";

        /// <summary>
        /// Gets or sets the storage service settings.
        /// Service implementer shall validate settings in the constructor and specify all settings in a single level JSON object as in the example below.
        /// </summary>
        /// <example>
        /// <code>
        /// {
        ///     ...
        ///     "settings": {
        ///         "endpoint": "1.2.3.4",
        ///         "accessKey": "monaideploy",
        ///         "accessToken": "mysecret",
        ///         "securedConnection": true,
        ///         "bucket": "myBucket"
        ///     }
        /// }
        /// </code>
        /// </example>
        [ConfigurationKeyName("settings")]
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
    }
}
