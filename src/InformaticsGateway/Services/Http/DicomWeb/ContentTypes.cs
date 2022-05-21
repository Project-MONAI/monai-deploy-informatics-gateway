// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Services.Http.DicomWeb
{
    public static class ContentTypes
    {
        public const string ApplicationDicom = "application/dicom";
        public const string ApplicationDicomJson = "application/dicom+json";
        public const string ApplicationDicomXml = "application/dicom+xml";
        public const string ApplicationOctetStream = "application/octet-stream";

        public const string MultipartRelated = "multipart/related";

        public const string TransferSyntax = "transfer-syntax";
        public const string Boundary = "boundary";
        public const string ContentType = "Content-Type";
    }
}
