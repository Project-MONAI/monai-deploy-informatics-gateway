/*
 * Copyright 2021-2022 MONAI Consortium
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

using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the configurations inside appsettings.json
        /// </summary>
        IConfigurationOptionAccessor Configurations { get; }

        /// <summary>
        /// Gets whether the configuration file exists or not.
        /// </summary>
        bool IsConfigExists { get; }

        /// <summary>
        /// Gets whether the configuration file has been intialized or not.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the Informatics Gateway environment.
        /// Extracts the appsettings.json file to ~/.mig for the application to consume.
        /// </summary>
        /// <returns></returns>
        Task Initialize(CancellationToken cancellationToken);

        /// <summary>
        /// Creates ~/.mig directory if not already exists.
        /// </summary>
        void CreateConfigDirectoryIfNotExist();
    }
}
