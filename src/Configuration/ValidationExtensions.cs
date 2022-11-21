/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            Guard.Against.Null(monaiApplicationEntity);

            validationErrors = new List<string>();

            var valid = true;
            valid &= IsAeTitleValid(monaiApplicationEntity.GetType().Name, monaiApplicationEntity.AeTitle, validationErrors);
            valid &= IsValidDicomTag(monaiApplicationEntity.GetType().Name, monaiApplicationEntity.Grouping, validationErrors);

            return valid;
        }

        public static bool IsValid(this DestinationApplicationEntity destinationApplicationEntity, out IList<string> validationErrors)
        {
            Guard.Against.Null(destinationApplicationEntity);

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
            Guard.Against.Null(sourceApplicationEntity);

            validationErrors = new List<string>();

            var valid = true;
            valid &= IsAeTitleValid(sourceApplicationEntity.GetType().Name, sourceApplicationEntity.AeTitle, validationErrors);
            valid &= IsValidHostNameIp(sourceApplicationEntity.AeTitle, sourceApplicationEntity.HostIp, validationErrors);

            return valid;
        }

        public static bool IsValidDicomTag(string source, string grouping, IList<string> validationErrors = null)
        {
            Guard.Against.NullOrWhiteSpace(source);

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
            Guard.Against.NullOrWhiteSpace(source);

            if (!string.IsNullOrWhiteSpace(aeTitle) &&
                aeTitle.Length <= 15 &&
                Regex.IsMatch(aeTitle, @"^[a-zA-Z0-9_\-]+$"))
            {
                return true;
            }

            validationErrors?.Add($"'{aeTitle}' is not a valid AE Title (source: {source}).");
            return false;
        }

        public static bool IsValidHostNameIp(string source, string hostIp, IList<string> validationErrors = null)
        {
            if (!string.IsNullOrWhiteSpace(hostIp) &&
                (Regex.IsMatch(hostIp, @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$") || // IP address
                 Regex.IsMatch(hostIp, @"^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\-]*[A-Za-z0-9])$"))) // Host/domain name
            {
                return true;
            }

            validationErrors?.Add($"Invalid host name/IP address '{hostIp}' specified for {source}.");
            return false;
        }

        public static bool IsPortValid(string source, int port, IList<string> validationErrors = null)
        {
            Guard.Against.NullOrWhiteSpace(source);

            if (port > 0 && port <= 65535) return true;

            validationErrors?.Add($"Invalid port number '{port}' specified for {source}.");
            return false;
        }
    }
}
