// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

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
