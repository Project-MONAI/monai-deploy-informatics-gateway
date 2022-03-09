// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;

namespace Monai.Deploy.InformaticsGateway.Common
{
    [Serializable]
    public class InsufficientStorageAvailableException : Exception
    {
        public InsufficientStorageAvailableException()
        {
        }

        public InsufficientStorageAvailableException(string message) : base(message)
        {
        }
    }
}
