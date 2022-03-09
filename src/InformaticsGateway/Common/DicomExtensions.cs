// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;
using FellowOakDicom;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public static class DicomExtensions
    {
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
                return null;
            }

            var dicomTransferSyntaxes = new List<DicomTransferSyntax>();

            foreach (var uid in uids)
            {
                dicomTransferSyntaxes.Add(DicomTransferSyntax.Lookup(DicomUID.Parse(uid)));
            }
            return dicomTransferSyntaxes.ToArray();
        }
    }
}
