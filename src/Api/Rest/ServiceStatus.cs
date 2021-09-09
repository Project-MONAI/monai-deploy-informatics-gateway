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

namespace Monai.Deploy.InformaticsGateway.Api.Rest
{
    /// <summary>
    /// Defines the state of a running service.
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// Unknown - default, during start up.
        /// </summary>
        Unknown,

        /// <summary>
        /// Service is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// Service is running.
        /// </summary>
        Running,

        /// <summary>
        /// Service has been cancelled by a cancellation token.
        /// </summary>
        Cancelled
    }
}
