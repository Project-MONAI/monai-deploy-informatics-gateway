// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    [Serializable]
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

        protected DicomWebClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            StatusCode = (HttpStatusCode?)info.GetValue("StatusCode", typeof(HttpStatusCode?));
            ResponseMessage = info.GetString("ResponseMessage");
            ResponseDataset = (DicomDataset)info.GetValue("ResponseDataset", typeof(DicomDataset));
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("StatusCode", StatusCode, typeof(HttpStatusCode?));
            info.AddValue("ResponseMessage", ResponseMessage);
            info.AddValue("ResponseDataset", ResponseDataset, typeof(DicomDataset));

            base.GetObjectData(info, context);
        }
    }
}
