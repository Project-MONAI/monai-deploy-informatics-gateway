/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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

using System;
using System.Collections.Generic;
using System.Text.Json;
using Ardalis.GuardClauses;
using FellowOakDicom;
using FellowOakDicom.Serialization;
using Monai.Deploy.InformaticsGateway.Configuration;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public static class DicomExtensions
    {
        private static readonly IList<DicomVR> DicomVrsToIgnore = new List<DicomVR>() { DicomVR.OB, DicomVR.OD, DicomVR.OF, DicomVR.OL, DicomVR.OV, DicomVR.OW, DicomVR.UN };

        /// <summary>
        /// Converts list of SOP Class UIDs to list of DicomTransferSyntax.
        /// DicomTransferSyntax.Parse internally throws DicomDataException if UID is invalid.
        /// </summary>
        /// <param name="uids">list of SOP Class UIDs</param>
        /// <returns>Array of DicomTransferSyntax or <c>null</c> if <c>uids</c> is null or empty.</returns>
        /// <exception cref="Dicom.DicomDataException">Thrown in the specified UID is not a transfer syntax type.</exception>
        public static DicomTransferSyntax[] ToDicomTransferSyntaxArray(this IEnumerable<string> uids)
        {
            if (uids.IsNullOrEmpty())
            {
                return Array.Empty<DicomTransferSyntax>();
            }

            var dicomTransferSyntaxes = new List<DicomTransferSyntax>();

            foreach (var uid in uids)
            {
                dicomTransferSyntaxes.Add(DicomTransferSyntax.Lookup(DicomUID.Parse(uid)));
            }
            return dicomTransferSyntaxes.ToArray();
        }

        public static string ToJson(this DicomFile dicomFile, DicomJsonOptions dicomJsonOptions, bool validateDicom)
        {
            Guard.Against.Null(dicomFile);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new DicomJsonConverter(
                writeTagsAsKeywords: false,
                autoValidate: validateDicom,
                numberSerializationMode: validateDicom ? NumberSerializationMode.AsNumber : NumberSerializationMode.PreferablyAsNumber));
            options.WriteIndented = false;

            if (dicomJsonOptions == DicomJsonOptions.Complete)
            {
                return JsonSerializer.Serialize(dicomFile.Dataset, options);
            }
            else
            {
                var dataset = dicomFile.Dataset.Clone();
                dataset.Remove(i => DicomVrsToIgnore.Contains(i.ValueRepresentation));
                return JsonSerializer.Serialize(dataset, options);
            }
        }
    }
}
