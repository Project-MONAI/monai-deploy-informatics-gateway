/*
 * Copyright 2022 MONAI Consortium
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

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Common
{
    internal static class DicomExtensions
    {
        public static string GenerateFileName(this DicomFile dicomFile) => GenerateFileName(dicomFile.Dataset);

        public static string GenerateFileName(this DicomDataset dicomDataset) => $"{dicomDataset.GetString(DicomTag.PatientID)}-{dicomDataset.GetString(DicomTag.SOPInstanceUID)}.dcm";

        public static string CalculateHash(this DicomFile dicomFile)
        {
            var bytes = new List<byte>();
            var data = dicomFile.Dataset.GetSingleStringValueAsBytes(DicomTag.PatientID);
            bytes.AddRange(data);
            data = dicomFile.Dataset.GetSingleStringValueAsBytes(DicomTag.StudyInstanceUID);
            bytes.AddRange(data);
            data = dicomFile.Dataset.GetSingleStringValueAsBytes(DicomTag.SeriesInstanceUID);
            bytes.AddRange(data);
            data = dicomFile.Dataset.GetSingleStringValueAsBytes(DicomTag.SOPInstanceUID);
            bytes.AddRange(data);

            var pixelData = DicomPixelData.Create(dicomFile.Dataset);
            for (int frame = 0; frame < pixelData.NumberOfFrames; frame++)
            {
                var buffer = pixelData.GetFrame(frame);
                bytes.AddRange(buffer.Data);
            }

            using var sha256Hash = SHA256.Create();
            var hash = sha256Hash.ComputeHash(bytes.ToArray());
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        public static byte[] GetSingleStringValueAsBytes(this DicomDataset dicomDataset, DicomTag dicomTag)
        {
            return Encoding.UTF8.GetBytes(dicomDataset.GetString(dicomTag));
        }
    }
}
