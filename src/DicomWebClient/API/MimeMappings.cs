// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Net.Http.Headers;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    public enum MimeType : short
    {
        Dicom = 0,
        DicomJson = 1,
        DicomXml = 2,
        OctetStreme = 3,
        ImageJpeg = 10,
        ImageGif = 11,
        ImagePng = 12,
        ImageJp2 = 13,
        ImageJpx = 14,
        VideoMpeg = 20,
        VideoMp4 = 21,
        VideoH265 = 22,
        VideoMpeg2 = 23
    }

    public static class MimeMappings
    {
        public const string MultiPartRelated = "multipart/related";

        public static readonly Dictionary<MimeType, string> MimeTypeMappings = new Dictionary<MimeType, string>()
        {
            { MimeType.Dicom, "application/dicom" },
            { MimeType.DicomJson, "application/dicom+json" },
            { MimeType.DicomXml, "application/dicom+xml" },
            { MimeType.OctetStreme, "application/octet-stream" },
            { MimeType.ImageJpeg, "image/jpeg" },
            { MimeType.ImageGif, "image/gif" },
            { MimeType.ImagePng, "image/png" },
            { MimeType.ImageJp2, "image/jp2" },
            { MimeType.ImageJpx, "image/jpx" },
            { MimeType.VideoMpeg, "video/mpeg" },
            { MimeType.VideoMp4, "video/mp4" },
            { MimeType.VideoH265, "video/H265" },
            { MimeType.VideoMpeg2, "video/mpeg2" },
        };

        public static readonly Dictionary<DicomUID, MimeType> SupportedMediaTypes = new Dictionary<DicomUID, MimeType>()
        {
            { DicomUID.ExplicitVRLittleEndian, MimeType.Dicom },
            { DicomUID.RLELossless, MimeType.Dicom },
            { DicomUID.JPEGBaseline8Bit, MimeType.Dicom },
            { DicomUID.JPEGExtended12Bit, MimeType.Dicom },
            { DicomUID.JPEGLossless, MimeType.Dicom },
            { DicomUID.JPEGLosslessSV1, MimeType.Dicom },
            { DicomUID.JPEGLSLossless, MimeType.Dicom },
            { DicomUID.JPEGLSNearLossless, MimeType.Dicom },
            { DicomUID.JPEG2000Lossless, MimeType.Dicom },
            { DicomUID.JPEG2000, MimeType.Dicom },
            { DicomUID.JPEG2000MCLossless, MimeType.Dicom },
            { DicomUID.JPEG2000MC, MimeType.Dicom },
            { DicomUID.MPEG2MPML, MimeType.Dicom },
            { DicomUID.MPEG2MPHL, MimeType.Dicom },
            { DicomUID.MPEG4HP41, MimeType.Dicom },
            { DicomUID.MPEG4HP41BD, MimeType.Dicom },
            { DicomUID.MPEG4HP422D, MimeType.Dicom },
            { DicomUID.MPEG4HP423D, MimeType.Dicom },
            { DicomUID.MPEG4HP42STEREO, MimeType.Dicom },
            { DicomUID.HEVCMP51, MimeType.Dicom },
            { DicomUID.HEVCM10P51, MimeType.Dicom }
        };

        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicom = new MediaTypeWithQualityHeaderValue(MimeTypeMappings[MimeType.Dicom]);
        public static readonly MediaTypeWithQualityHeaderValue MediaTypeApplicationDicomJson = new MediaTypeWithQualityHeaderValue(MimeTypeMappings[MimeType.DicomJson]);

        public static bool IsValidMediaType(DicomTransferSyntax transferSyntax)
        {
            return SupportedMediaTypes.ContainsKey(transferSyntax.UID);
        }
    }
}
