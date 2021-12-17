// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
