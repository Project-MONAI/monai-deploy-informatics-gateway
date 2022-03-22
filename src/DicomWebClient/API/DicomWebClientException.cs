// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    [Serializable]
    public class DicomWebClientException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ResponseMessage { get; }

        public DicomWebClientException(HttpStatusCode? statusCode, string responseMessage, Exception innerException)
            : base(responseMessage, innerException)
        {
            StatusCode = statusCode;
            ResponseMessage = responseMessage;
        }

        protected DicomWebClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            StatusCode = (HttpStatusCode?)info.GetValue(nameof(StatusCode), typeof(HttpStatusCode?));
            ResponseMessage = info.GetString(nameof(ResponseMessage));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue(nameof(StatusCode), StatusCode, typeof(HttpStatusCode?));
            info.AddValue(nameof(ResponseMessage), ResponseMessage);

            base.GetObjectData(info, context);
        }
    }
}
