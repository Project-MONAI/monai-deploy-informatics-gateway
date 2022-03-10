// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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
