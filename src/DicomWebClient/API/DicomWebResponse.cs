// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Net;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public class DicomWebResponse<T>
    {
        public HttpStatusCode StatusCode { get; }
        public T Result { get; }

        public DicomWebResponse(HttpStatusCode statusCode, T result)
        {
            StatusCode = statusCode;
            Result = result;
        }
    }
}
