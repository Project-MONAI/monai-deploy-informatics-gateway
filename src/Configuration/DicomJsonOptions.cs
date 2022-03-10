// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2021 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    public enum DicomJsonOptions
    {
        /// <summary>
        /// Do not write DICOM to JSON
        /// </summary>
        None,

        /// <summary>
        /// Writes DICOM to JSON exception VR types of OB, OD, OF, OL, OV, OW, and UN.
        /// </summary>
        IgnoreOthers,

        /// <summary>
        /// Writes DICOM to JSON
        /// </summary>
        Complete
    }
}
