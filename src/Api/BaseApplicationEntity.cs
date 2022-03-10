// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// DICOM Application Entity or AE.
    /// </summary>
    /// <remarks>
    /// * [Application Entity](http://www.otpedia.com/entryDetails.cfm?id=137)
    /// </remarks>
    public class BaseApplicationEntity
    {
        /// <summary>
        /// Gets or sets the unique name used to identify a DICOM application entity.
        /// This value must be unique.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  Gets or sets the AE Title (AET) used to identify itself in a DICOM association.
        /// </summary>
        public string AeTitle { get; set; }

        /// <summary>
        /// Gets or set the host name or IP address of the AE Title.
        /// </summary>
        public string HostIp { get; set; }

        public BaseApplicationEntity()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = AeTitle;
        }
    }
}
