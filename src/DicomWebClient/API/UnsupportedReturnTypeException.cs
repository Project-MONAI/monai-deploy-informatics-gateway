// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public class UnsupportedReturnTypeException : Exception
    {
        public UnsupportedReturnTypeException(string message) : base(message)
        {
        }
    }
}
