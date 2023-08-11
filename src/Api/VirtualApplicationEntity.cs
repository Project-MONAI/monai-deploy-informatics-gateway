/*
 * Copyright 2023 MONAI Consortium
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
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// Virtual Application Entity (VAE)
    /// An VAE identifies a service or application similar to a DIMSE AE but is
    /// designed to be used for DICOMWeb.
    ///
    /// For example: users can configure VAEs on DICOMWeb STOW-RS endpoints to enable
    /// data input plug-ins, <see cref="IInputDataPlugin"/>. This allows different plug-ins
    /// to be associated with each VAE for data manipulation, etc...
    ///
    /// </summary>
    public class VirtualApplicationEntity : MongoDBEntityBase
    {
        /// <summary>
        /// Gets or sets the name of a MONAI DICOM application entity.
        /// This value must be unique.
        /// </summary>
        [Key, Column(Order = 0)]
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the virtual AE TItle which is used as the URL fragment in the DICOMWeb STOW-RS endpoint.
        /// E.g. POST /dicomweb/u/{aet}/studies where {aet} is the value from this property.
        /// </summary>
        public string VirtualAeTitle { get; set; } = default!;

        /// <summary>
        /// Optional field to map AE to one or more workflows.
        /// </summary>
        public List<string> Workflows { get; set; } = default!;

        /// <summary>
        /// Optional list of data input plug-in type names to be executed by the <see cref="IInputDataPluginEngine"/>.
        /// </summary>
        public List<string> PluginAssemblies { get; set; } = default!;

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

        public VirtualApplicationEntity()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = VirtualAeTitle;
            }

            Workflows ??= new List<string>();

            PluginAssemblies ??= new List<string>();
        }

        public override string ToString()
        {
            return $"Name: {Name}/AET: {VirtualAeTitle}";
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
    }
}
