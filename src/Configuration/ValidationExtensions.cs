// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using System.Linq;
using Ardalis.GuardClauses;
using FellowOakDicom;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public static class ValidationExtensions
    {
        private static readonly DicomTag[] AllowedGroupingTags = new[] { DicomTag.PatientID, DicomTag.StudyInstanceUID, DicomTag.SeriesInstanceUID };

        public static bool IsValid(this MonaiApplicationEntity monaiApplicationEntity, out IList<string> validationErrors)
        {
            Guard.Against.Null(monaiApplicationEntity, nameof(monaiApplicationEntity));

            validationErrors = new List<string>();

            var valid = true;
            valid &= IsAeTitleValid(monaiApplicationEntity.GetType().Name, monaiApplicationEntity.AeTitle, validationErrors);
            valid &= IsValidDicomTag(monaiApplicationEntity.GetType().Name, monaiApplicationEntity.Grouping, validationErrors);

            return valid;
        }

        public static bool IsValid(this DestinationApplicationEntity destinationApplicationEntity, out IList<string> validationErrors)
        {
            Guard.Against.Null(destinationApplicationEntity, nameof(destinationApplicationEntity));

            validationErrors = new List<string>();

            var valid = true;
            valid &= !string.IsNullOrWhiteSpace(destinationApplicationEntity.Name);
            valid &= IsAeTitleValid(destinationApplicationEntity.GetType().Name, destinationApplicationEntity.AeTitle, validationErrors);
            valid &= IsValidHostNameIp(destinationApplicationEntity.AeTitle, destinationApplicationEntity.HostIp, validationErrors);
            valid &= IsPortValid(destinationApplicationEntity.GetType().Name, destinationApplicationEntity.Port, validationErrors);

            return valid;
        }

        public static bool IsValid(this SourceApplicationEntity sourceApplicationEntity, out IList<string> validationErrors)
        {
            Guard.Against.Null(sourceApplicationEntity, nameof(sourceApplicationEntity));

            validationErrors = new List<string>();

            var valid = true;
            valid &= IsAeTitleValid(sourceApplicationEntity.GetType().Name, sourceApplicationEntity.AeTitle, validationErrors);
            valid &= IsValidHostNameIp(sourceApplicationEntity.AeTitle, sourceApplicationEntity.HostIp, validationErrors);

            return valid;
        }

        public static bool IsValidDicomTag(string source, string grouping, IList<string> validationErrors = null)
        {
            Guard.Against.NullOrWhiteSpace(source, nameof(source));

            try
            {
                var dicomTag = DicomTag.Parse(grouping);

                if (AllowedGroupingTags.Contains(dicomTag))
                {
                    return true;
                }
                validationErrors?.Add($"'{grouping}' is not a valid DICOM tag (source: {source}).");
                return false;
            }
            catch (DicomDataException ex)
            {
                validationErrors?.Add($"'{grouping}' is not a valid DICOM tag (source: {source}, error: {ex.Message}).");
                return false;
            }
        }

        public static bool IsAeTitleValid(string source, string aeTitle, IList<string> validationErrors = null)
        {
            Guard.Against.NullOrWhiteSpace(source, nameof(source));

            if (!string.IsNullOrWhiteSpace(aeTitle) && aeTitle.Length <= 15) return true;

            validationErrors?.Add($"'{aeTitle}' is not a valid AE Title (source: {source}).");
            return false;
        }

        public static bool IsValidHostNameIp(string source, string hostIp, IList<string> validationErrors = null)
        {
            if (!string.IsNullOrWhiteSpace(hostIp)) return true;

            validationErrors?.Add($"Invalid host name/IP address '{hostIp}' specified for {source}.");
            return false;
        }

        public static bool IsPortValid(string source, int port, IList<string> validationErrors = null)
        {
            Guard.Against.NullOrWhiteSpace(source, nameof(source));

            if (port > 0 && port <= 65535) return true;

            validationErrors?.Add($"Invalid port number '{port}' specified for {source}.");
            return false;
        }
    }
}
