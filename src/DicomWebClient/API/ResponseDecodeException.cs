// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public class ResponseDecodeException : Exception
    {
        public ResponseDecodeException(string message) : base(message)
        {
        }
    }
}
