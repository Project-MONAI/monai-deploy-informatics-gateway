// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public class DicomWebClientException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ResponseMessage { get; }
        public DicomDataset ResponseDataset { get; }

        public DicomWebClientException(HttpStatusCode? statusCode, string responseMessage, Exception innerException)
            : base(responseMessage, innerException)
        {
            StatusCode = statusCode;
            ResponseMessage = responseMessage;
        }
    }
}
