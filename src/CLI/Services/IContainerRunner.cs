// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    /// <summary>
    /// Represents the state of a running application in the orchestration engine.
    /// </summary>
    public class RunnerState
    {
        /// <summary>
        /// Indicates whether the application is running or not.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// ID of the running application provided by the orchestration engine.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Shorter version of the ID, with 12 characters.
        /// </summary>
        public string IdShort
        {
            get
            {
                return Id.Substring(0, Math.Min(12, Id.Length));
            }
        }
    }

    /// <summary>
    /// Represents the image version the container runner detected
    /// </summary>
    public class ImageVersion
    {
        /// <summary>
        /// Version or label of the application/image detected.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Unique ID provided by the orchestration engine.
        /// </summary>
        /// <value></value>
        public string Id { get; set; }

        /// <summary>
        /// Shorter version of the ID, with 12 characters.
        /// </summary>
        public string IdShort
        {
            get
            {
                var id = Id.Replace("sha256:", "");
                return id.Substring(0, Math.Min(12, id.Length));
            }
        }

        /// <summary>
        /// Date tiem the image was created.
        /// </summary>
        public DateTime Created { get; set; }
    }

    public interface IContainerRunner
    {
        Task<RunnerState> IsApplicationRunning(ImageVersion imageVersion, CancellationToken cancellationToken = default);

        Task<ImageVersion> GetLatestApplicationVersion(CancellationToken cancellationToken = default);

        Task<ImageVersion> GetLatestApplicationVersion(string version, CancellationToken cancellationToken = default);

        Task<IList<ImageVersion>> GetApplicationVersions(CancellationToken cancellationToken = default);

        Task<IList<ImageVersion>> GetApplicationVersions(string version, CancellationToken cancellationToken = default);

        Task<bool> StartApplication(ImageVersion imageVersion, CancellationToken cancellationToken = default);

        Task<bool> StopApplication(RunnerState runnerState, CancellationToken cancellationToken = default);
    }
}
