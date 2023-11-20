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
using System.Security.Claims;
using Monai.Deploy.InformaticsGateway.Api.Storage;

namespace Monai.Deploy.InformaticsGateway.Api.Models
{
    /// <summary>
    /// DICOM Application Entity or AE.
    /// </summary>
    /// <remarks>
    /// * [Application Entity](http://www.otpedia.com/entryDetails.cfm?id=137)
    /// </remarks>
    public class BaseApplicationEntity : MongoDBEntityBase
    {
        /// <summary>
        /// Gets or sets the unique name used to identify a DICOM application entity.
        /// This value must be unique.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        ///  Gets or sets the AE Title (AET) used to identify itself in a DICOM association.
        /// </summary>
        public string AeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or set the host name or IP address of the AE Title.
        /// </summary>
        public string HostIp { get; set; } = default!;

        /// <summary>
        /// Gets or set the user who created the DICOM entity.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or set the most recent user who updated the DICOM entity.
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or set the most recent date time the DICOM entity was updated.
        /// </summary>
        public DateTime? DateTimeUpdated { get; set; }

        public BaseApplicationEntity()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = AeTitle;
        }

        public void SetAuthor(ClaimsPrincipal user, EditMode editMode)
        {
            if (editMode == EditMode.Update)
            {
                DateTimeUpdated = DateTime.UtcNow;
            }

            if (editMode == EditMode.Create)
            {
                CreatedBy = user.Identity?.Name;
            }
            else if (editMode == EditMode.Update)
            {
                UpdatedBy = user.Identity?.Name;
            }
        }

        public override string ToString()
        {
            return $"Name: {Name}/AET: {AeTitle}/Host: {HostIp}";
        }
    }
}
